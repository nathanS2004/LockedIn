using System;
using UnityEngine;
using LockedIn;

public static class RoomCameraProjectionChecks
{
    private static int checks;
    private static void Near(float actual, float expected, string label)
    {
        if (Math.Abs(actual - expected) > 0.00001f) throw new Exception(label + ": " + actual + " != " + expected);
        checks++;
    }

    public static int Main()
    {
        var intrinsics = RoomCameraProjection.Intrinsics(new Vector2(600, 610), new Vector2(640, 480),
            new Vector2(1280, 960), new Vector2(1280, 960));
        Vector2 center = RoomCameraProjection.Project(new Vector3(0, 0, 1), intrinsics);
        Near(center.x, 0.5f, "Optical axis x");
        Near(center.y, 0.5f, "Optical axis y");
        Near(intrinsics.x, 600f / 1280, "Horizontal focal scale");
        Near(intrinsics.y, 610f / 960, "Vertical focal scale");
        Vector2 first = RoomCameraProjection.Project(new Vector3(0.2f, -0.1f, 1), intrinsics);
        Vector2 scaled = RoomCameraProjection.Project(new Vector3(0.4f, -0.2f, 2), intrinsics);
        Near(first.x, scaled.x, "Projection ray scale x");
        Near(first.y, scaled.y, "Projection ray scale y");
        var small = RoomCameraProjection.Intrinsics(new Vector2(600, 610), new Vector2(640, 480),
            new Vector2(1280, 960), new Vector2(320, 240));
        Near(small.x, intrinsics.x, "Same aspect resolution x");
        Near(small.y, intrinsics.y, "Same aspect resolution y");
        var cropped = RoomCameraProjection.Intrinsics(new Vector2(600, 610), new Vector2(640, 480),
            new Vector2(1280, 960), new Vector2(960, 960));
        Near(cropped.x, 600f / 960, "Square horizontal sensor crop");
        Near(cropped.y, 610f / 960, "Square vertical sensor crop");
        Near(cropped.z, 0.5f, "Cropped principal x");
        Near(cropped.w, 0.5f, "Cropped principal y");
        // Opposite RGB camera offsets must produce different source samples for a shared point.
        Vector2 left = RoomCameraProjection.Project(new Vector3(0.03f, 0, 0.5f), intrinsics);
        Vector2 right = RoomCameraProjection.Project(new Vector3(-0.03f, 0, 0.5f), intrinsics);
        Near(left.x - right.x, 0.12f * intrinsics.x, "Per-eye camera baseline");
        Console.WriteLine("PASS: " + checks + " room camera projection checks");
        return 0;
    }
}
