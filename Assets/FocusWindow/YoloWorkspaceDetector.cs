// The setup command enables this symbol only after installing the actual SDKs and model.
#if LOCKEDIN_AI
using System;
using System.Collections;
using System.Collections.Generic;
using Meta.XR;
using Unity.InferenceEngine;
using UnityEngine;

namespace LockedIn
{
    internal sealed class YoloWorkspaceDetector : IWorkspaceDetector
    {
        private const float StrongScreenConfidence = 0.65f;
        private const float ScreenAimTolerance = 0.06f;
        private readonly Transform head;
        private readonly ScreenDepthSelection depth;
        private GameObject cameraObject;
        private PassthroughCameraAccess cameraAccess;
        private Worker worker;
        private Tensor<float> input;
        private Pose capturePose;
        private Vector3 captureHeadPosition;
        private Quaternion captureHeadRotation;
        private bool disposed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register() => WorkspaceDetectorFactory.Create =
            (head, depth) => new YoloWorkspaceDetector(head, depth);

        private YoloWorkspaceDetector(Transform head, ScreenDepthSelection depth)
        {
            this.head = head;
            this.depth = depth;
        }

        public IEnumerator Detect(Ray aim, Action<string> status, Action<WorkspaceDetection> complete, WorkspaceScanDebug debug)
        {
#if !UNITY_ANDROID || UNITY_EDITOR
            complete(new WorkspaceDetection { Message = "Test AI on the Quest headset" });
            yield break;
#else
            if (!OVRPlugin.initialized)
                throw new InvalidOperationException("Meta XR Feature did not initialize. Run LockedIn AI setup and rebuild.");
            var asset = Resources.Load<ModelAsset>("LockedIn/yolov9sentis");
            if (asset == null) throw new InvalidOperationException("YOLO model missing. Run LockedIn AI setup.");
            status("Starting the camera and YOLO");
            cameraObject = new GameObject("LockedIn workspace camera");
            cameraObject.SetActive(false);
            cameraAccess = cameraObject.AddComponent<PassthroughCameraAccess>();
            cameraAccess.enabled = false;
            cameraAccess.CameraPosition = PassthroughCameraAccess.CameraPositionType.Left;
            cameraAccess.RequestedResolution = new Vector2Int(1280, 960);
            cameraAccess.MaxFramerate = 30;
            cameraObject.SetActive(true);
            cameraAccess.enabled = true;
            float deadline = Time.realtimeSinceStartup + 12f;
            while (!cameraAccess.IsPlaying)
            {
                if (Time.realtimeSinceStartup > deadline)
                    throw new InvalidOperationException("Camera timed out. Check headset camera permission and Meta XR Feature.");
                yield return null;
            }
            Model model = ModelLoader.Load(asset);
            int height = model.inputs[0].shape.Get(2);
            int width = model.inputs[0].shape.Get(3);
            // Match Meta's small quantized sample model. Inference runs only during selection.
            worker = new Worker(model, BackendType.CPU);
            input = new Tensor<float>(new TensorShape(1, 3, height, width));
            status("Scanning screen and keyboard");
            yield return new WaitForEndOfFrame();
            if (!cameraAccess.IsPlaying) throw new InvalidOperationException("Camera stopped before capture.");
            captureHeadPosition = head.position;
            captureHeadRotation = head.rotation;
            // The SDK pose is in tracking space. Apply the same XR Origin/Camera Offset as the headset.
            Pose trackingPose = cameraAccess.GetCameraPose();
            Transform space = head.parent;
            capturePose = new Pose(space.TransformPoint(trackingPose.position), space.rotation * trackingPose.rotation);
            if (Quaternion.Dot(capturePose.rotation, capturePose.rotation) < 0.5f)
                throw new InvalidOperationException("Passthrough camera pose is unavailable.");
            Texture cameraFrame = cameraAccess.GetTexture();
            debug?.Capture(cameraFrame, width, height);
            TextureConverter.ToTensor(cameraFrame, input,
                new TextureTransform().SetDimensions(width, height, 3));
            IEnumerator schedule = worker.ScheduleIterable(input);
            int scheduled = 0;
            while (schedule.MoveNext())
                if (++scheduled % 12 == 0) yield return null;

            var boxes = worker.PeekOutput(0) as Tensor<float>;
            var classes = worker.PeekOutput(1) as Tensor<int>;
            var scores = worker.PeekOutput(2) as Tensor<float>;
            if (boxes == null || classes == null || scores == null)
                throw new InvalidOperationException("Unexpected YOLO outputs. Use the pinned Meta sample model.");
            boxes.ReadbackRequest();
            classes.ReadbackRequest();
            scores.ReadbackRequest();
            deadline = Time.realtimeSinceStartup + 20f;
            while (!boxes.IsReadbackRequestDone() || !classes.IsReadbackRequestDone() || !scores.IsReadbackRequestDone())
            {
                if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("YOLO inference timed out.");
                yield return null;
            }
            using var boxData = boxes.ReadbackAndClone();
            using var classData = classes.ReadbackAndClone();
            using var scoreData = scores.ReadbackAndClone();
            if (boxData.shape.rank != 2 || boxData.shape[1] != 4 ||
                boxData.shape[0] != classData.shape[0] || boxData.shape[0] != scoreData.shape[0])
                throw new InvalidOperationException("YOLO output shapes do not match the pinned model.");
            var candidates = new List<WorkspaceBox>();
            for (int i = 0; i < classData.shape[0]; i++)
            {
                int id = classData[i];
                float score = scoreData[i];
                if (!WorkspaceBoxFilter.IsRelevant(id) || (score < 0.35f && debug == null)) continue;
                Rect rect = Rect.MinMaxRect(Mathf.Clamp01(boxData[i, 0] / width), Mathf.Clamp01(boxData[i, 1] / height),
                    Mathf.Clamp01(boxData[i, 2] / width), Mathf.Clamp01(boxData[i, 3] / height));
                var box = new WorkspaceBox(id, score, rect);
                debug?.Boxes.Add(box);
                if (score >= 0.35f) candidates.Add(box);
            }
            List<WorkspaceBox> detections = WorkspaceBoxFilter.Suppress(candidates);
            debug?.SetModelResults(detections);
            debug?.Record($"motion after inference: {Vector3.Distance(head.position, captureHeadPosition):F3} m / {Quaternion.Angle(head.rotation, captureHeadRotation):F1} deg");
            // Depth is sampled after inference. Refuse a stale image if the user moved significantly.
            if (Vector3.Distance(head.position, captureHeadPosition) > 0.08f ||
                Quaternion.Angle(head.rotation, captureHeadRotation) > 10f)
            {
                complete(new WorkspaceDetection { Message = "Head moved during scan. Try again." });
                yield break;
            }
            status("Fitting the detected workspace");
            yield return FitDetections(detections, aim, complete, debug);
#endif
        }

        private Ray ImageRay(Vector2 topLeftUv) =>
            cameraAccess.ViewportPointToRay(new Vector2(topLeftUv.x, 1f - topLeftUv.y), capturePose);

        private IEnumerator FitDetections(List<WorkspaceBox> detections, Ray aim,
            Action<WorkspaceDetection> complete, WorkspaceScanDebug debug)
        {
            int screenCount = 0;
            foreach (WorkspaceBox box in detections)
                if (box.ClassId != 66) screenCount++;

            if (screenCount == 0)
            {
                complete(new WorkspaceDetection { Message = "No screen detected in the camera view" });
                yield break;
            }

            WorkspaceBox screen = default;
            ScreenDepthSelection.ScreenVolume? fittedScreen = null;
            int fittedCount = 0;
            float bestMiss = float.PositiveInfinity;
            float bestDistance = float.PositiveInfinity;
            foreach (WorkspaceBox box in detections)
            {
                if (box.ClassId == 66) continue;
                int number = debug?.BoxNumber(box) ?? 0;
                Rect screenRect = box.Rect;
                if (box.ClassId == 63) screenRect.height *= 0.62f;
                ScreenDepthSelection.ScreenVolume? fit = null;
                yield return depth.FindScreen(ImageRay(screenRect.center), capturePose.position,
                    capturePose.rotation * Vector3.right, volume => fit = volume);
                debug?.Record($"screen candidate #{number}: depth edge scan passed={fit.HasValue}");
                if (!fit.HasValue && WorkspaceGeometry.TryFit(screenRect, false, ImageRay, depth, out var fallback, debug))
                    fit = fallback;

                if (Vector3.Distance(head.position, captureHeadPosition) > 0.08f ||
                    Quaternion.Angle(head.rotation, captureHeadRotation) > 10f)
                {
                    complete(new WorkspaceDetection { Message = "Head moved while fitting. Try again." });
                    yield break;
                }
                debug?.Record($"screen candidate #{number}: depth fit passed={fit.HasValue}");
                if (!fit.HasValue) continue;
                fittedCount++;

                // Depth independently verifies the only strong screen. The review still
                // requires confirmation, so the controller can stay at normal reach.
                if (screenCount == 1 && box.Score >= StrongScreenConfidence)
                {
                    screen = box;
                    fittedScreen = fit;
                    debug?.Record($"selected screen candidate #{number}: single strong detection verified by depth");
                    break;
                }

                if (!WorkspaceGeometry.TryMeasureScreenAim(aim, fit.Value, out float distance, out float miss))
                {
                    debug?.Record($"screen candidate #{number}: controller ray did not reach the screen plane");
                    continue;
                }
                debug?.Record($"screen candidate #{number}: controller miss {miss:F3} m, ray distance {distance:F3} m");
                if (miss > ScreenAimTolerance || miss > bestMiss ||
                    (miss == bestMiss && distance >= bestDistance)) continue;

                // A hit has zero miss. If two screens overlap along the ray, the nearer one wins.
                bestMiss = miss;
                bestDistance = distance;
                screen = box;
                fittedScreen = fit;
            }

            if (!fittedScreen.HasValue)
            {
                string message = fittedCount == 0 ? "Screen recognized; depth edges unclear" :
                    "Point at the screen you want and scan again";
                complete(new WorkspaceDetection { Message = message });
                yield break;
            }

            if (!float.IsPositiveInfinity(bestMiss))
                debug?.Record($"selected screen candidate #{debug.BoxNumber(screen)}: controller ray, miss {bestMiss:F3} m");
            bool laptop = screen.ClassId == 63;
            var result = new WorkspaceDetection();
            result.Volumes.Add(fittedScreen.Value);
            Vector3 screenCenter = WorkspaceGeometry.Center(fittedScreen.Value);
            ScreenDepthSelection.ScreenVolume? keyboard = null;
            float closest = 1f;
            foreach (WorkspaceBox box in detections)
            {
                if (box.ClassId != 66) continue;
                if (box.Rect.center.y < screen.Rect.center.y - 0.1f)
                {
                    debug?.Record($"keyboard candidate #{debug.BoxNumber(box)} rejected: above screen in image");
                    continue;
                }
                debug?.Record($"fitting keyboard candidate #{debug.BoxNumber(box)}");
                if (!WorkspaceGeometry.TryFit(box.Rect, true, ImageRay, depth, out var volume, debug)) continue;
                Vector3 center = WorkspaceGeometry.Center(volume);
                float distance = Vector3.Distance(center, screenCenter);
                if (distance >= closest || center.y > screenCenter.y + 0.08f)
                {
                    debug?.Record($"keyboard candidate rejected: distance {distance:F3} m, height offset {center.y - screenCenter.y:F3} m");
                    continue;
                }
                debug?.Record($"keyboard candidate #{debug.BoxNumber(box)} passed pairing checks");
                keyboard = volume;
                closest = distance;
                yield return null;
            }
            if (!keyboard.HasValue && laptop)
            {
                // A laptop class often contains its base. This is an explicitly labelled geometric estimate.
                Rect baseRect = Rect.MinMaxRect(screen.Rect.xMin, screen.Rect.yMin + screen.Rect.height * 0.65f,
                    screen.Rect.xMax, screen.Rect.yMax);
                debug?.Write("trying estimated laptop base");
                if (WorkspaceGeometry.TryFit(baseRect, true, ImageRay, depth, out var volume, debug) &&
                    Vector3.Distance(WorkspaceGeometry.Center(volume), screenCenter) < 0.65f &&
                    WorkspaceGeometry.Center(volume).y < screenCenter.y)
                {
                    keyboard = volume;
                    result.EstimatedKeyboard = true;
                }
            }
            if (keyboard.HasValue)
            {
                result.Volumes.Add(keyboard.Value);
                result.HasKeyboard = true;
            }
            result.Message = result.EstimatedKeyboard ? "Screen + estimated laptop base" :
                result.HasKeyboard ? "Screen + keyboard found" : "Screen found; keyboard not found";
            complete(result);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (cameraAccess != null) cameraAccess.enabled = false;
            if (cameraObject != null) UnityEngine.Object.Destroy(cameraObject);
            // Cancellation can occur during inference. Complete queued work before releasing its inputs.
            worker?.Dispose();
            worker = null;
            input?.Dispose();
            input = null;
        }
    }
}
#endif
