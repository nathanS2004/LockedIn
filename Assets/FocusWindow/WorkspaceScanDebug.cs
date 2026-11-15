using System;
using System.Collections.Generic;
using UnityEngine;

namespace LockedIn
{
    // Owned by the scan flow so the frozen frame can outlive the camera until review ends.
    internal sealed class WorkspaceScanDebug : IDisposable
    {
        private static int nextId;
        private readonly int id = ++nextId;
        private readonly float started = Time.realtimeSinceStartup;

        public readonly List<WorkspaceBox> Boxes = new List<WorkspaceBox>();
        public List<WorkspaceBox> Kept { get; private set; }
        public RenderTexture Frame { get; private set; }
        public int Id => id;

        public WorkspaceScanDebug() => Write("started");
        public void Write(string message) => Debug.Log($"[LockedIn Scan #{id}] {message}");
        public void Record(FormattableString message) => Write(FormattableString.Invariant(message));
        public int BoxNumber(WorkspaceBox box) => Boxes.IndexOf(box) + 1;
        public static string Label(int classId) => classId == 66 ? "Keyboard" : classId == 63 ? "Laptop" : "Screen/TV";

        public void Capture(Texture texture, int modelWidth, int modelHeight)
        {
            if (texture == null) return;
            Record($"camera {texture.width}x{texture.height}, input {modelWidth}x{modelHeight}");
            const int width = 480;
            int height = Mathf.Max(1, Mathf.RoundToInt(width * (float)texture.height / texture.width));
            RenderTexture copy = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Graphics.Blit(texture, copy);
                Frame = copy;
            }
            catch (Exception error)
            {
                RenderTexture.ReleaseTemporary(copy);
                Write("preview capture failed: " + error.GetType().Name);
            }
            finally
            {
                RenderTexture.active = previous;
            }
        }

        public void SetModelResults(List<WorkspaceBox> kept)
        {
            Kept = kept;
            Record($"model returned {Boxes.Count} relevant boxes, {kept.Count} passed filtering");
            for (int i = 0; i < Boxes.Count; i++)
            {
                var box = Boxes[i];
                string stage = kept.Contains(box) ? "kept" : box.Score < 0.35f ? "below cutoff" : "filtered";
                Record($"box #{i + 1}: {Label(box.ClassId)} {box.Score:F3}, {stage}, image rect {box.Rect.ToString("F3")}");
            }
        }

        public void Finish(string title, WorkspaceDetection result)
        {
            int count = result?.Volumes.Count ?? 0;
            Record($"result: {title}; volumes={count}; estimatedKeyboard={result?.EstimatedKeyboard ?? false}; elapsed={(Time.realtimeSinceStartup - started):F2}s");
            for (int i = 0; i < count; i++)
            {
                var volume = result.Volumes[i];
                Record($"border {i + 1}: center={WorkspaceGeometry.Center(volume).ToString("F3")}, size={(volume.Max - volume.Min).ToString("F3")} m");
            }
        }

        public void Dispose()
        {
            if (Frame == null) return;
            RenderTexture.ReleaseTemporary(Frame);
            Frame = null;
        }
    }
}
