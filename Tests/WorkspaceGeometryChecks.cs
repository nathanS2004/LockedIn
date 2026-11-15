using System;
using System.Collections.Generic;
using UnityEngine;
using LockedIn;

internal static class WorkspaceGeometryChecks
{
    private static int checks;
    private static void Check(bool result, string message)
    {
        if (!result) throw new Exception(message);
        checks++;
    }

    public static int Main()
    {
        var laptop = new WorkspaceBox(63, 0.95f, new Rect(0.2f, 0.2f, 0.5f, 0.6f));
        var duplicate = new WorkspaceBox(62, 0.7f, new Rect(0.21f, 0.21f, 0.5f, 0.6f));
        var keyboard = new WorkspaceBox(66, 0.9f, new Rect(0.25f, 0.55f, 0.4f, 0.2f));
        var weak = new WorkspaceBox(66, 0.2f, new Rect(0.1f, 0.1f, 0.2f, 0.2f));
        var person = new WorkspaceBox(0, 0.99f, new Rect(0, 0, 1, 1));
        var kept = WorkspaceBoxFilter.Suppress(new List<WorkspaceBox> { laptop, duplicate, keyboard, weak, person });
        Check(kept.Count == 2 && kept[0].ClassId == 63 && kept[1].ClassId == 66,
            "Keep nested keyboard, suppress duplicate screen, reject weak/irrelevant detections.");
        Check(WorkspaceBoxFilter.Suppress(new List<WorkspaceBox>()).Count == 0, "Empty detection result.");
        Check(WorkspaceBoxFilter.Suppress(new List<WorkspaceBox> {
            new WorkspaceBox(66, float.NaN, new Rect(0.2f, 0.2f, 0.3f, 0.2f)) }).Count == 0, "Reject NaN confidence.");
        Check(WorkspaceBoxFilter.Suppress(new List<WorkspaceBox> {
            new WorkspaceBox(66, 0.8f, new Rect(float.NaN, 0, 0.3f, 0.2f)) }).Count == 0, "Reject NaN box.");

        Func<Vector2, Ray> imageRay = uv => new Ray(new Vector3(0, 0.9f, 0),
            new Vector3(uv.x - 0.5f, 0.5f - uv.y, 1));
        Rect screenRect = new Rect(0.2f, 0.2f, 0.6f, 0.4f);
        var screenPoints = SamplePlane(screenRect, imageRay, new Plane(Vector3.back, new Vector3(0, 0.9f, 1)));
        Check(WorkspaceGeometry.TryFitPoints(screenRect, false, imageRay, screenPoints, out var screen), "Fit vertical screen.");
        Check(!WorkspaceGeometry.TryFitPoints(screenRect, true, imageRay, screenPoints, out _), "Reject vertical keyboard.");
        Check(Mathf.Abs(Vector3.Dot(screen.Right, screen.Up)) < 0.001f, "Screen axes orthogonal.");
        Check(Mathf.Abs(WorkspaceGeometry.Center(screen).z - 1) < 0.01f, "Screen retains physical depth.");

        Vector3 screenCenter = WorkspaceGeometry.Center(screen);
        Vector3 controller = new Vector3(0.5f, 0.35f, 0.65f);
        var offsetAim = new Ray(controller, (screenCenter - controller).normalized);
        Check(WorkspaceGeometry.TryMeasureScreenAim(offsetAim, screen, out float hitDistance, out float hitMiss) &&
            hitDistance > 0 && hitMiss < 0.001f, "An off-axis controller can hit the screen without matching the camera direction.");
        var nextScreen = new ScreenDepthSelection.ScreenVolume(screen.Origin + Vector3.right,
            screen.Right, screen.Up, screen.Normal, screen.Min, screen.Max);
        Check(WorkspaceGeometry.TryMeasureScreenAim(offsetAim, nextScreen, out _, out float otherMiss) &&
            otherMiss > 0.06f, "The ray aimed at one screen does not select its neighbor.");
        Vector3 nearEdge = screen.Origin + screen.Right * (screen.Max.x + 0.04f) +
            screen.Up * (screen.Min.y + screen.Max.y) * 0.5f;
        Check(WorkspaceGeometry.TryMeasureScreenAim(new Ray(controller, (nearEdge - controller).normalized),
            screen, out _, out float edgeMiss) && Mathf.Abs(edgeMiss - 0.04f) < 0.001f, "Measure edge misses in meters.");
        Check(!WorkspaceGeometry.TryMeasureScreenAim(new Ray(controller, -offsetAim.direction), screen, out _, out _),
            "A controller pointing away cannot select a screen behind it.");
        Check(!WorkspaceGeometry.TryMeasureScreenAim(new Ray(controller, screen.Right), screen, out _, out _),
            "A ray parallel to the display does not select it.");

        Rect keyboardRect = new Rect(0.3f, 0.7f, 0.4f, 0.2f);
        var keyboardPoints = SamplePlane(keyboardRect, imageRay, new Plane(Vector3.up, new Vector3(0, 0.65f, 0)));
        Check(WorkspaceGeometry.TryFitPoints(keyboardRect, true, imageRay, keyboardPoints, out var board), "Fit horizontal keyboard.");
        Check(!WorkspaceGeometry.TryFitPoints(keyboardRect, false, imageRay, keyboardPoints, out _), "Reject horizontal screen.");
        Check(Mathf.Abs(WorkspaceGeometry.Center(board).y - 0.65f) < 0.01f, "Keyboard stays on table plane.");
        keyboardPoints[0] += new Vector3(0, 0.4f, 0);
        Check(WorkspaceGeometry.TryFitPoints(keyboardRect, true, imageRay, keyboardPoints, out _), "Tolerate one hand/depth outlier.");
        Check(!WorkspaceGeometry.TryFitPoints(screenRect, false, imageRay, new List<Vector3> { Vector3.zero }, out _), "Reject insufficient depth.");
        var draft = new WorkspaceBorderDraft(screen, board);
        var expanded = WorkspaceBorderDraft.Adjust(board, BorderHandle.Bottom, new Vector2(0.2f, -0.08f));
        Check(Mathf.Abs(expanded.Min.y - board.Min.y + 0.08f) < 0.001f &&
            expanded.Max == board.Max && expanded.Origin == board.Origin, "Resize a keyboard edge on its plane.");
        var clamped = WorkspaceBorderDraft.Adjust(board, BorderHandle.Bottom, new Vector2(0, 3));
        Check(Mathf.Abs(clamped.Max.y - clamped.Min.y - 0.06f) < 0.001f, "Dragging through the far edge leaves a valid border.");
        var moved = WorkspaceBorderDraft.Adjust(board, BorderHandle.Move, new Vector2(0.03f, -0.02f));
        Check(Mathf.Abs(Vector3.Dot(moved.Origin - board.Origin, board.Normal)) < 0.001f &&
            moved.Min == board.Min && moved.Max == board.Max, "Moving preserves size and stays on plane.");
        draft.Remember();
        draft.Set(WorkspaceBorder.Keyboard, expanded);
        draft.Remember();
        draft.Set(WorkspaceBorder.Keyboard, null);
        draft.Undo();
        Check(draft.Keyboard.HasValue && draft.Keyboard.Value.Min == expanded.Min && draft.Screen.Value.Origin == screen.Origin,
            "Undo removes only the most recent edit and retains the original screen.");
        draft.Undo();
        Check(draft.Keyboard.Value.Min == board.Min && !draft.CanUndo, "Undo restores the original detected keyboard.");
        Console.WriteLine("PASS: " + checks + " workspace filtering and geometry checks.");
        return 0;
    }

    private static List<Vector3> SamplePlane(Rect rect, Func<Vector2, Ray> ray, Plane plane)
    {
        var points = new List<Vector3>();
        for (int y = 0; y < 3; y++)
        for (int x = 0; x < 3; x++)
        {
            Vector2 uv = new Vector2(Mathf.Lerp(rect.xMin, rect.xMax, 0.2f + x * 0.3f),
                Mathf.Lerp(rect.yMin, rect.yMax, 0.2f + y * 0.3f));
            Ray r = ray(uv);
            if (plane.Raycast(r, out float d)) points.Add(r.GetPoint(d));
        }
        return points;
    }
}
