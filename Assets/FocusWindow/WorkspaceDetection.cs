using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LockedIn
{
    // The provider is registered only after the optional camera and inference packages are installed.
    internal interface IWorkspaceDetector : IDisposable
    {
        IEnumerator Detect(Ray aim, Action<string> status, Action<WorkspaceDetection> complete, WorkspaceScanDebug debug);
    }

    internal sealed class WorkspaceDetection
    {
        public string Message;
        public readonly List<ScreenDepthSelection.ScreenVolume> Volumes = new List<ScreenDepthSelection.ScreenVolume>();
        public bool HasKeyboard;
        public bool EstimatedKeyboard;
    }

    internal static class WorkspaceDetectorFactory
    {
        public static Func<Transform, ScreenDepthSelection, IWorkspaceDetector> Create;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Create = null;
    }

    internal readonly struct WorkspaceBox
    {
        // Image coordinates: top-left origin, normalized to [0,1]. COCO class IDs.
        public readonly int ClassId;
        public readonly float Score;
        public readonly Rect Rect;
        public WorkspaceBox(int classId, float score, Rect rect)
        {
            ClassId = classId;
            Score = score;
            Rect = rect;
        }
    }

    internal static class WorkspaceBoxFilter
    {
        public static bool IsRelevant(int id) => id == 62 || id == 63 || id == 66;

        public static List<WorkspaceBox> Suppress(List<WorkspaceBox> boxes)
        {
            boxes.Sort((a, b) => b.Score.CompareTo(a.Score));
            var kept = new List<WorkspaceBox>();
            foreach (var box in boxes)
            {
                if (!IsRelevant(box.ClassId) || !Finite(box.Score) || box.Score < 0.35f ||
                    !Finite(box.Rect.xMin) || !Finite(box.Rect.yMin) ||
                    !Finite(box.Rect.xMax) || !Finite(box.Rect.yMax) ||
                    box.Rect.width < 0.025f || box.Rect.height < 0.025f)
                    continue;
                bool duplicate = false;
                foreach (var other in kept)
                {
                    // Do not suppress a keyboard nested within a laptop detection.
                    bool sameKind = (box.ClassId == 66) == (other.ClassId == 66);
                    if (sameKind && IoU(box.Rect, other.Rect) > 0.45f)
                    {
                        duplicate = true;
                        break;
                    }
                }
                if (!duplicate) kept.Add(box);
                if (kept.Count == 12) break;
            }
            return kept;
        }

        private static bool Finite(float x) => !float.IsNaN(x) && !float.IsInfinity(x);

        internal static float IoU(Rect a, Rect b)
        {
            float intersection = Mathf.Max(0, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin)) *
                                 Mathf.Max(0, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin));
            return intersection / Mathf.Max(0.00001f, a.width * a.height + b.width * b.height - intersection);
        }
    }
}
