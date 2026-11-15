using System.Collections.Generic;
using UnityEngine;
using Volume = LockedIn.ScreenDepthSelection.ScreenVolume;

namespace LockedIn
{
    internal enum WorkspaceBorder { Screen, Keyboard }
    internal enum BorderHandle { Move, Left, Right, Bottom, Top }

    internal sealed class WorkspaceBorderDraft
    {
        private Volume? screen, keyboard;
        private readonly Stack<(Volume?, Volume?)> history = new Stack<(Volume?, Volume?)>();

        public WorkspaceBorderDraft(Volume screen, Volume? keyboard)
        {
            this.screen = screen;
            this.keyboard = keyboard;
        }

        public Volume? Screen => screen;
        public Volume? Keyboard => keyboard;
        public bool CanUndo => history.Count > 0;

        public Volume? Get(WorkspaceBorder region) => region == WorkspaceBorder.Screen ? screen : keyboard;
        public void Remember() => history.Push((screen, keyboard));

        public void Set(WorkspaceBorder region, Volume? value)
        {
            if (region == WorkspaceBorder.Screen) screen = value;
            else keyboard = value;
        }

        public void Undo()
        {
            if (history.Count > 0) (screen, keyboard) = history.Pop();
        }

        public static bool Project(Ray ray, Volume volume, out Vector2 point, out float distance)
        {
            point = default;
            if (!new Plane(volume.Normal, volume.Origin).Raycast(ray, out distance) || distance > 3f)
                return false;
            Vector3 offset = ray.GetPoint(distance) - volume.Origin;
            point = new Vector2(Vector3.Dot(offset, volume.Right), Vector3.Dot(offset, volume.Up));
            return true;
        }

        public static bool PickHandle(Volume v, Vector2 point, out BorderHandle handle)
        {
            handle = BorderHandle.Move;
            const float tolerance = 0.025f;
            if (point.x < v.Min.x - tolerance || point.x > v.Max.x + tolerance ||
                point.y < v.Min.y - tolerance || point.y > v.Max.y + tolerance) return false;

            float closest = Mathf.Min(0.045f, Mathf.Min(v.Max.x - v.Min.x, v.Max.y - v.Min.y) * 0.22f);
            ChooseEdge(Mathf.Abs(point.x - v.Min.x), BorderHandle.Left, ref closest, ref handle);
            ChooseEdge(Mathf.Abs(point.x - v.Max.x), BorderHandle.Right, ref closest, ref handle);
            ChooseEdge(Mathf.Abs(point.y - v.Min.y), BorderHandle.Bottom, ref closest, ref handle);
            ChooseEdge(Mathf.Abs(point.y - v.Max.y), BorderHandle.Top, ref closest, ref handle);
            return true;
        }

        private static void ChooseEdge(float distance, BorderHandle edge, ref float closest, ref BorderHandle handle)
        {
            if (distance > closest) return;
            closest = distance;
            handle = edge;
        }

        public static Volume Adjust(Volume v, BorderHandle handle, Vector2 delta)
        {
            const float minimumSize = 0.06f;
            Vector3 min = v.Min, max = v.Max;
            switch (handle)
            {
                case BorderHandle.Left: min.x = Mathf.Min(min.x + delta.x, max.x - minimumSize); break;
                case BorderHandle.Right: max.x = Mathf.Max(max.x + delta.x, min.x + minimumSize); break;
                case BorderHandle.Bottom: min.y = Mathf.Min(min.y + delta.y, max.y - minimumSize); break;
                case BorderHandle.Top: max.y = Mathf.Max(max.y + delta.y, min.y + minimumSize); break;
                case BorderHandle.Move:
                    return new Volume(v.Origin + v.Right * delta.x + v.Up * delta.y, v.Right, v.Up,
                        v.Normal, min, max);
            }
            return new Volume(v.Origin, v.Right, v.Up, v.Normal, min, max);
        }
    }
}
