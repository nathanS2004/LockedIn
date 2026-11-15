using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LockedIn
{
    public sealed partial class FocusWindow
    {
        private const string CameraPermission = "horizonos.permission.HEADSET_CAMERA";
        private bool selectingWorkspace;
        private bool waitingForWorkspacePermission;
        private bool hasKeyboardWindow;
        private ScreenDepthSelection.ScreenVolume keyboardWindow;
        private WorkspaceDetection workspacePreview;
        private IWorkspaceDetector workspaceDetector;
        private Coroutine workspaceScan;

        private bool debugWorkspaceScan;
        private WorkspaceScanDebug scanDebug;

        private void BeginWorkspaceDetection(bool debug = false)
        {
            roomBlur?.Stop();
            CancelAutoSelection();
            CancelWorkspaceDetection();
            placingBorder = false;
            menu.Hide();
            selectingWorkspace = true;
            debugWorkspaceScan = debug;
            waitingForWorkspacePermission = false;
            if (WorkspaceDetectorFactory.Create == null)
            {
                menu.ShowCalibration("AI setup needed in Unity", "Run LockedIn > Set Up AI Workspace Detection, then rebuild. B for manual.");
                return;
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!HasWorkspacePermissions())
            {
                waitingForWorkspacePermission = true;
                menu.ShowCalibration("Allow camera and scene access", "The scan stays on your headset. B for manual, grip to cancel.");
                UnityEngine.Android.Permission.RequestUserPermissions(new[] { CameraPermission, ScenePermission });
                return;
            }
#endif
            ShowWorkspaceAim();
        }

        private static bool HasWorkspacePermissions()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return UnityEngine.Android.Permission.HasUserAuthorizedPermission(CameraPermission) &&
                   UnityEngine.Android.Permission.HasUserAuthorizedPermission(ScenePermission);
#else
            return true;
#endif
        }

        private void ShowWorkspaceAim() => menu.ShowCalibration("Look at your screen and keyboard",
            "Aim at your screen, then trigger to scan. Keep both in view. B for manual.");

        private void UpdateWorkspaceDetection(bool hasRay, Ray aim, bool pressed)
        {
            RenderMask(false, default, true);
            if (WorkspaceDetectorFactory.Create == null || !HasWorkspacePermissions()) return;
            if (waitingForWorkspacePermission)
            {
                waitingForWorkspacePermission = false;
                ShowWorkspaceAim();
            }
            if (raycasts == null)
            {
                menu.ShowCalibration("Depth unavailable", "Press B to place a border manually.");
                return;
            }
            if (!raycasts.enabled)
            {
                raycasts.enabled = true;
                ShowWorkspaceAim();
            }
            if (workspaceScan != null) return;
            if (workspacePreview != null && toggle.WasPressedThisFrame())
            {
                var screen = workspacePreview.Volumes[0];
                screenOrigin = screen.Origin;
                screenRight = screen.Right;
                screenUp = screen.Up;
                screenNormal = screen.Normal;
                windowMin = screen.Min;
                windowMax = screen.Max;
                hasKeyboardWindow = workspacePreview.Volumes.Count > 1;
                if (hasKeyboardWindow) keyboardWindow = workspacePreview.Volumes[1];
                CancelWorkspaceDetection();
                FinishBorder();
                return;
            }
            if (!pressed || !hasRay) return;
            workspacePreview = null;
            ClearScanDebug();
            if (debugWorkspaceScan) scanDebug = new WorkspaceScanDebug();
            try
            {
                workspaceDetector?.Dispose();
                workspaceDetector = WorkspaceDetectorFactory.Create(transform, screenSelection);
                workspaceScan = StartCoroutine(RunWorkspaceScan(aim));
            }
            catch (Exception error)
            {
                WorkspaceFailed(error);
            }
        }

        private IEnumerator RunWorkspaceScan(Ray aim)
        {
            // Start on the next frame so even an immediate failure can clear the Coroutine field.
            yield return null;
            var stack = new Stack<IEnumerator>();
            stack.Push(workspaceDetector.Detect(aim,
                stage =>
                {
                    scanDebug?.Write(stage);
                    menu.ShowCalibration(stage, "Keep your head still. Grip cancels; B switches to manual.");
                },
                result =>
                {
                    workspacePreview = result != null && result.Volumes.Count > 0 ? result : null;
                    string title = result?.Message ?? "No workspace found";
                    scanDebug?.Finish(title, result);
                    menu.ShowScanDebug(scanDebug);
                    menu.ShowCalibration(title, workspacePreview == null
                        ? "Trigger to retry, B for manual. Keep the screen and keyboard in view."
                        : "A to keep these borders. Trigger to scan again; grip to cancel.");
                }, scanDebug));
            try
            {
                while (stack.Count > 0)
                {
                    IEnumerator top = stack.Peek();
                    bool hasNext = false;
                    object next = null;
                    Exception failure = null;
                    try
                    {
                        hasNext = top.MoveNext();
                        if (hasNext) next = top.Current;
                    }
                    catch (Exception error) { failure = error; }
                    if (failure != null)
                    {
                        WorkspaceFailed(failure);
                        yield break;
                    }
                    if (!hasNext)
                    {
                        (stack.Pop() as IDisposable)?.Dispose();
                        continue;
                    }
                    if (next is IEnumerator nested) stack.Push(nested);
                    else yield return next;
                }
            }
            finally
            {
                while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
                workspaceScan = null;
                workspaceDetector?.Dispose();
                workspaceDetector = null;
            }
        }

        private void WorkspaceFailed(Exception error)
        {
            scanDebug?.Write(error.ToString());
            scanDebug?.Finish("Exception: " + error.GetType().Name, null);
            menu.ShowScanDebug(scanDebug);
            Debug.LogError("LockedIn AI scan failed: " + error, this);
            workspacePreview = null;
            workspaceScan = null;
            menu.ShowCalibration("AI scan unavailable", "Trigger to retry, or B for manual. The Unity log has details.");
        }

        private void CancelWorkspaceDetection()
        {
            if (workspaceScan != null)
            {
                scanDebug?.Write("canceled");
                StopCoroutine(workspaceScan);
            }
            workspaceScan = null;
            workspaceDetector?.Dispose();
            workspaceDetector = null;
            workspacePreview = null;
            ClearScanDebug();
            if (selectingWorkspace) menu?.HideCalibration();
            selectingWorkspace = false;
        }

        private void ClearScanDebug()
        {
            menu?.ShowScanDebug(null);
            scanDebug?.Dispose();
            scanDebug = null;
        }

        private void OnApplicationPause(bool paused)
        {
            SuspendEnvironment(paused);
            if (paused && workspaceScan != null) CancelWorkspaceDetection();
        }

        private void SetVolume(string prefix, ScreenDepthSelection.ScreenVolume volume)
        {
            mask.SetVector(prefix + "Origin", volume.Origin);
            mask.SetVector(prefix + "Right", volume.Right);
            mask.SetVector(prefix + "Up", volume.Up);
            mask.SetVector(prefix + "Normal", volume.Normal);
            mask.SetVector(prefix + "Min", volume.Min);
            mask.SetVector(prefix + "Max", volume.Max);
        }
    }
}
