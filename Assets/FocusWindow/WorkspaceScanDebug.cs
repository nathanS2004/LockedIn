using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LockedIn
{
    // Owned by the scan flow so the frozen frame can outlive the camera until review ends.
    internal sealed class WorkspaceScanDebug : IDisposable
    {
        private const string LogFileName = "lockedin-scan-log.txt";
        private static readonly string SessionId = Guid.NewGuid().ToString("N").Substring(0, 8);
        private static int nextId;
        private StreamWriter writer;
        private readonly int id = ++nextId;
        private readonly float started = Time.realtimeSinceStartup;

        public readonly List<WorkspaceBox> Boxes = new List<WorkspaceBox>();
        public List<WorkspaceBox> Kept { get; private set; }
        public RenderTexture Frame { get; private set; }
        public int Id => id;

        public WorkspaceScanDebug()
        {
            try
            {
                writer = new StreamWriter(Path.Combine(Application.persistentDataPath, LogFileName), append: true);
                Write($"started at {DateTime.UtcNow:O} UTC; app version {Application.version}");
                FlushLog();
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                ReportLogFailure(error);
            }
        }

        private string Prefix => $"[LockedIn Scan #{id}] [{SessionId}] ";

        public void Write(string message)
        {
            try
            {
                writer?.WriteLine(Prefix + message);
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                ReportLogFailure(error);
            }
        }

        private void FlushLog()
        {
            try
            {
                writer?.Flush();
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                ReportLogFailure(error);
            }
        }

        private void CloseLog()
        {
            StreamWriter openWriter = writer;
            writer = null;
            try
            {
                openWriter?.Dispose();
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                Debug.LogWarning(Prefix + "Could not close scan log: " + error.Message);
            }
        }

        private void ReportLogFailure(Exception error)
        {
            Debug.LogWarning(Prefix + "Could not save scan details: " + error.Message);
            CloseLog();
        }

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
                Record($"box #{i + 1}: {Label(box.ClassId)} {box.Score:G9}, {stage}, image xywh=({box.Rect.x:G9}, {box.Rect.y:G9}, {box.Rect.width:G9}, {box.Rect.height:G9})");
            }
            FlushLog();
        }

        public void Finish(string title, WorkspaceDetection result)
        {
            int count = result?.Volumes.Count ?? 0;
            string keyboard = result?.EstimatedKeyboard == true ? "estimated laptop base" :
                result?.HasKeyboard == true ? "YOLO keyboard with depth fit" : "none";
            string summary = FormattableString.Invariant(
                $"result: {title}; volumes={count}; keyboard={keyboard}; elapsed={(Time.realtimeSinceStartup - started):F2}s");
            Write(summary);
            Debug.Log(Prefix + summary);
            for (int i = 0; i < count; i++)
            {
                var volume = result.Volumes[i];
                Record($"border {i + 1}: center={WorkspaceGeometry.Center(volume).ToString("F3")}, size={(volume.Max - volume.Min).ToString("F3")} m");
            }
            CloseLog();
        }

        public void Dispose()
        {
            CloseLog();
            if (Frame == null) return;
            RenderTexture.ReleaseTemporary(Frame);
            Frame = null;
        }
    }
}
