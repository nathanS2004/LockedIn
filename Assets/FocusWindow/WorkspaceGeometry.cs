using System;
using System.Collections.Generic;
using UnityEngine;

namespace LockedIn
{
    internal static class WorkspaceGeometry
    {
        // Fit a physical surface to depth samples inside a detection, rather than a head-facing billboard.
        public static bool TryFit(Rect rect, bool keyboard, Func<Vector2, Ray> imageRay,
                                  ScreenDepthSelection depth, out ScreenDepthSelection.ScreenVolume volume,
                                  WorkspaceScanDebug debug = null)
        {
            var points = new List<Vector3>();
            for (int y = 0; y < 3; y++)
            for (int x = 0; x < 3; x++)
            {
                Vector2 uv = new Vector2(Mathf.Lerp(rect.xMin, rect.xMax, 0.2f + 0.3f * x),
                                         Mathf.Lerp(rect.yMin, rect.yMax, 0.2f + 0.3f * y));
                Ray ray = imageRay(uv);
                if (depth.TryDepthPoint(ray, out Vector3 point) &&
                    Vector3.Distance(ray.origin, point) is > 0.18f and < 2.5f)
                    points.Add(point);
            }
            bool passed = TryFitPoints(rect, keyboard, imageRay, points, out volume);
            debug?.Record($"depth fit ({(keyboard ? "keyboard/base" : "screen")}): {points.Count}/9 valid depth samples, passed={passed}");
            return passed;
        }

        internal static bool TryFitPoints(Rect rect, bool keyboard, Func<Vector2, Ray> imageRay,
                                          List<Vector3> points, out ScreenDepthSelection.ScreenVolume volume)
        {
            volume = default;
            if (points.Count < 5) return false;
            Vector3 center = default, normal = default;
            int bestCount = 0;
            // Nine samples are small enough for exhaustive three-point consensus.
            for (int a = 0; a < points.Count - 2; a++)
            for (int b = a + 1; b < points.Count - 1; b++)
            for (int c = b + 1; c < points.Count; c++)
            {
                Vector3 cross = Vector3.Cross(points[b] - points[a], points[c] - points[a]);
                if (cross.magnitude < 0.001f) continue;
                Vector3 n = cross.normalized;
                float vertical = Mathf.Abs(Vector3.Dot(n, Vector3.up));
                if (keyboard ? vertical < 0.85f : vertical > 0.65f) continue;
                int count = 0;
                Vector3 sum = Vector3.zero;
                foreach (Vector3 point in points)
                {
                    if (Mathf.Abs(Vector3.Dot(point - points[a], n)) > 0.025f) continue;
                    count++;
                    sum += point;
                }
                if (count <= bestCount) continue;
                bestCount = count;
                center = sum / count;
                normal = n;
            }
            if (bestCount < 5 || bestCount < Mathf.CeilToInt(points.Count * 0.65f)) return false;

            Ray centerRay = imageRay(rect.center);
            if (Vector3.Dot(normal, centerRay.direction) > 0) normal = -normal;
            var plane = new Plane(normal, center);
            Ray leftRay = imageRay(new Vector2(rect.xMin, rect.center.y));
            Ray rightRay = imageRay(new Vector2(rect.xMax, rect.center.y));
            if (!HitPlane(plane, leftRay, out Vector3 left) || !HitPlane(plane, rightRay, out Vector3 rightPoint))
                return false;
            Vector3 right = (rightPoint - left).normalized;
            Vector3 up = Vector3.Cross(normal, right).normalized;
            if (right.sqrMagnitude < 0.9f || up.sqrMagnitude < 0.9f) return false;
            Vector3 min = Vector3.one * float.PositiveInfinity;
            Vector3 max = Vector3.one * float.NegativeInfinity;
            for (int corner = 0; corner < 4; corner++)
            {
                Vector2 uv = new Vector2(corner % 2 == 0 ? rect.xMin : rect.xMax,
                                         corner < 2 ? rect.yMin : rect.yMax);
                if (!HitPlane(plane, imageRay(uv), out Vector3 point)) return false;
                Vector3 delta = point - center;
                var local = new Vector3(Vector3.Dot(delta, right), Vector3.Dot(delta, up), 0);
                min = Vector3.Min(min, local);
                max = Vector3.Max(max, local);
            }
            Vector3 size = max - min;
            if (size.x < 0.12f || size.y < 0.06f ||
                size.x > (keyboard ? 0.85f : 1.5f) || size.y > (keyboard ? 0.65f : 1f)) return false;
            Vector3 padding = new Vector3(0.025f, 0.025f, 0.025f);
            volume = new ScreenDepthSelection.ScreenVolume(center, right, up, normal, min - padding, max + padding);
            return true;
        }

        private static bool HitPlane(Plane plane, Ray ray, out Vector3 point)
        {
            point = default;
            if (!plane.Raycast(ray, out float distance) || distance < 0.15f || distance > 2.7f)
                return false;
            point = ray.GetPoint(distance);
            return true;
        }

        // The controller ray and camera rays originate at different points. Compare
        // the controller's plane intersection with the fitted screen in world space.
        internal static bool TryMeasureScreenAim(Ray aim, ScreenDepthSelection.ScreenVolume screen,
                                                 out float distance, out float miss)
        {
            miss = float.PositiveInfinity;
            var plane = new Plane(screen.Normal, screen.Origin);
            if (!plane.Raycast(aim, out distance) || distance > 3f)
                return false;

            Vector3 offset = aim.GetPoint(distance) - screen.Origin;
            float x = Vector3.Dot(offset, screen.Right);
            float y = Vector3.Dot(offset, screen.Up);
            float outsideX = x - Mathf.Clamp(x, screen.Min.x, screen.Max.x);
            float outsideY = y - Mathf.Clamp(y, screen.Min.y, screen.Max.y);
            miss = new Vector2(outsideX, outsideY).magnitude;
            return true;
        }

        public static Vector3 Center(ScreenDepthSelection.ScreenVolume volume) =>
            volume.Origin + volume.Right * (volume.Min.x + volume.Max.x) * 0.5f +
            volume.Up * (volume.Min.y + volume.Max.y) * 0.5f +
            volume.Normal * (volume.Min.z + volume.Max.z) * 0.5f;
    }
}
