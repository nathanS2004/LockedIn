using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
#if LOCKEDIN_AI
using Meta.XR;
using Meta.XR.EnvironmentDepth;
#endif

namespace LockedIn
{
    // Filters actual RGB images. Depth reprojection is mandatory; never displays a flat camera quad.
    internal sealed class RoomPassthroughBlur : IDisposable
    {
        private readonly Material mask;
        private readonly Transform head;
        public string Status { get; private set; } = "Select a workspace";
#if LOCKEDIN_AI
        private readonly PassthroughCameraAccess[] cameras = new PassthroughCameraAccess[2];
        private readonly RenderTexture[] scratch = new RenderTexture[2], blurred = new RenderTexture[2];
        private readonly DateTime[] timestamps = new DateTime[2];
        private readonly float[] updated = new float[2];
        private readonly DateTime[] blurredAt = new DateTime[2];
        private readonly float[] blurredStrength = new float[2];
        private static readonly int[] ColorIDs = { Shader.PropertyToID("_RoomLeft"), Shader.PropertyToID("_RoomRight") };
        private static readonly int[] PoseIDs = { Shader.PropertyToID("_RoomWorldToLeft"), Shader.PropertyToID("_RoomWorldToRight") };
        private static readonly int[] IntrinsicsIDs = { Shader.PropertyToID("_RoomLeftIntrinsics"), Shader.PropertyToID("_RoomRightIntrinsics") };
        private readonly Matrix4x4[] depthProjection = new Matrix4x4[2], depthInverse = new Matrix4x4[2];
        private readonly List<Matrix4x4> matrices = new List<Matrix4x4>(2);
        private EnvironmentDepthManager depth;
        private GameObject captureRoot;
        private Material filter;
        private RenderTexture ambientScratch, ambientFill;
        private bool ownsDepth, requestedPermission, subscribed;
        private float strength, startedAt;
        private bool active;
        private Texture previousDepthTexture;
        private float depthUpdated;
#endif

        public RoomPassthroughBlur(Material mask, Transform head)
        {
            this.mask = mask;
            this.head = head;
        }

        public void Update(bool enabled, float amount)
        {
            mask.SetFloat("_RoomBlurReady", 0);
#if LOCKEDIN_AI && UNITY_ANDROID && !UNITY_EDITOR
            if (!enabled || amount <= 0)
            {
                Stop();
                Status = enabled ? "Turn knob to blur" : "";
                return;
            }
            strength = amount;
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission("horizonos.permission.HEADSET_CAMERA") ||
                !UnityEngine.Android.Permission.HasUserAuthorizedPermission("com.oculus.permission.USE_SCENE"))
            {
                Status = "Allow camera + scene access";
                if (!requestedPermission)
                {
                    requestedPermission = true;
                    UnityEngine.Android.Permission.RequestUserPermissions(new[] {
                        "horizonos.permission.HEADSET_CAMERA", "com.oculus.permission.USE_SCENE" });
                }
                return;
            }
            if (!active)
            {
                if (XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.MultiPass)
                { Status = "Room Blur needs single-pass XR - clear"; return; }
                if (!OVRPlugin.initialized || !EnvironmentDepthManager.IsSupported)
                { Status = "Camera/depth unavailable - clear"; return; }
                Start();
                if (!active) return;
            }
            if (Time.realtimeSinceStartup - startedAt > 12 &&
                (!cameras[0].IsPlaying || !cameras[1].IsPlaying || !depth.IsDepthAvailable))
                Status = "Camera/depth not ready - clear";
#else
            Status = "Room Blur requires Quest camera/depth";
#endif
        }

#if LOCKEDIN_AI
        private void Start()
        {
            Shader shader = Resources.Load<Shader>("RoomCameraBlur");
            if (shader == null || !shader.isSupported)
            { Status = "Blur shader unavailable - clear"; return; }
            if (filter == null) filter = new Material(shader);
            captureRoot = new GameObject("Room Blur stereo capture");
            captureRoot.SetActive(false);
            captureRoot.transform.SetParent(head, false);
            depth = UnityEngine.Object.FindAnyObjectByType<EnvironmentDepthManager>();
            ownsDepth = depth == null;
            if (ownsDepth)
            {
                depth = captureRoot.AddComponent<EnvironmentDepthManager>();
                depth.CustomTrackingSpace = head.parent;
                depth.OcclusionShadersMode = OcclusionShadersMode.None;
            }
            for (int eye = 0; eye < 2; eye++)
            {
                var go = new GameObject(eye == 0 ? "Left RGB" : "Right RGB");
                go.transform.SetParent(captureRoot.transform, false);
                cameras[eye] = go.AddComponent<PassthroughCameraAccess>();
                cameras[eye].CameraPosition = eye == 0 ? PassthroughCameraAccess.CameraPositionType.Left : PassthroughCameraAccess.CameraPositionType.Right;
                cameras[eye].RequestedResolution = new Vector2Int(1280, 960);
                cameras[eye].MaxFramerate = 30;
                timestamps[eye] = default;
                blurredAt[eye] = default;
                updated[eye] = 0;
            }
            captureRoot.SetActive(true);
            active = true;
            startedAt = Time.realtimeSinceStartup;
            Status = "Starting stereo camera + depth";
            Application.onBeforeRender += BeforeRender;
            subscribed = true;
        }

        [BeforeRenderOrder(100)] // EnvironmentDepthManager publishes matrices at order 0.
        private void BeforeRender()
        {
            mask.SetFloat("_RoomBlurReady", 0);
            if (!active || strength <= 0 || !Application.isFocused || depth == null || !depth.IsDepthAvailable)
                return;
            var headset = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (!headset.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) || !tracked)
            { Status = "Tracking lost - clear"; return; }
            float now = Time.realtimeSinceStartup;
            for (int eye = 0; eye < 2; eye++)
            {
                var camera = cameras[eye];
                if (camera == null || !camera.IsPlaying || camera.Timestamp == default)
                    return;
                if (timestamps[eye] != camera.Timestamp)
                { timestamps[eye] = camera.Timestamp; updated[eye] = now; }
                if (now - updated[eye] > 0.2f)
                { Status = "Camera stale - clear"; return; }
            }
            if (Math.Abs((timestamps[0] - timestamps[1]).TotalMilliseconds) > 35)
            { Status = "Waiting for stereo frames - clear"; return; }
            var texture = Shader.GetGlobalTexture("_EnvironmentDepthTexture");
            Shader.GetGlobalMatrixArray("_EnvironmentDepthReprojectionMatrices", matrices);
            if (texture == null || matrices.Count != 2) return;
            // The SDK publishes rotating native depth buffers only when a new frame arrives.
            if (texture != previousDepthTexture)
            { previousDepthTexture = texture; depthUpdated = now; }
            if (now - depthUpdated > 0.2f)
            { Status = "Depth stale - clear"; return; }
            RenderTexture previousTarget = RenderTexture.active;
            bool previousSrgbWrite = GL.sRGBWrite;
            try
            {
                GL.sRGBWrite = false; // Camera is sampled as sRGB; blur buffers store linear RGB.
                for (int eye = 0; eye < 2; eye++)
                {
                    if (Mathf.Abs(matrices[eye].determinant) < 0.000001f) return;
                    depthProjection[eye] = matrices[eye];
                    depthInverse[eye] = matrices[eye].inverse;
                    if (!PrepareEye(eye)) return;
                }
            }
            finally { RenderTexture.active = previousTarget; GL.sRGBWrite = previousSrgbWrite; }
            mask.SetTexture("_RoomDepth", texture);
            mask.SetMatrixArray("_RoomDepthProjection", depthProjection);
            mask.SetMatrixArray("_RoomDepthInverse", depthInverse);
            mask.SetFloat("_RoomBlurMix", Mathf.SmoothStep(0, 1, strength / 0.08f));
            mask.SetFloat("_RoomBlurReady", 1);
            Status = "Experimental - soft surrounding fill";
        }

        private bool PrepareEye(int eye)
        {
            var camera = cameras[eye];
            Texture source = camera.GetTexture();
            if (source == null || source.width <= 0 || source.height <= 0 || head.parent == null)
                return false;
            Pose pose = camera.GetCameraPose(); // Tracking-space camera pose at its capture timestamp.
            Quaternion rotation = pose.rotation;
            if (rotation.x * rotation.x + rotation.y * rotation.y + rotation.z * rotation.z + rotation.w * rotation.w < 0.5f)
                return false;
            int width = 320, height = Mathf.Max(1, Mathf.RoundToInt(width * (float)source.height / source.width));
            if (scratch[eye] == null || scratch[eye].height != height)
            {
                Release(ref scratch[eye]); Release(ref blurred[eye]);
                scratch[eye] = NewTexture(width, height);
                blurred[eye] = NewTexture(width, height);
            }
            // Separable 9-tap Gaussian. Knob changes the radius, not an overlay's darkness.
            if (blurredAt[eye] != timestamps[eye] || Mathf.Abs(blurredStrength[eye] - strength) > 0.001f)
            {
                float radius = strength * 12f / 4f;
                filter.SetVector("_BlurStep", new Vector4(radius / width, 0, 0, 0));
                Graphics.Blit(source, scratch[eye], filter);
                filter.SetVector("_BlurStep", new Vector4(0, radius / height, 0, 0));
                Graphics.Blit(scratch[eye], blurred[eye], filter);
                blurredAt[eye] = timestamps[eye];
                blurredStrength[eye] = strength;
                if (eye == 0)
                {
                    // A shared, very low-detail color field fills regions the RGB
                    // cameras cannot see. This is ambient approximation, not geometry.
                    if (ambientScratch == null)
                    {
                        ambientScratch = NewTexture(16, 12);
                        ambientFill = NewTexture(16, 12);
                    }
                    float spread = Mathf.Lerp(0.03f, 0.12f, strength);
                    filter.SetVector("_BlurStep", new Vector4(spread, 0, 0, 0));
                    Graphics.Blit(blurred[0], ambientScratch, filter);
                    filter.SetVector("_BlurStep", new Vector4(0, spread, 0, 0));
                    Graphics.Blit(ambientScratch, ambientFill, filter);
                    mask.SetTexture("_RoomAmbient", ambientFill);
                }
            }
            Matrix4x4 worldPose = head.parent.localToWorldMatrix * Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one);
            var intrinsics = camera.Intrinsics;
            mask.SetTexture(ColorIDs[eye], blurred[eye]);
            mask.SetMatrix(PoseIDs[eye], worldPose.inverse);
            mask.SetVector(IntrinsicsIDs[eye], RoomCameraProjection.Intrinsics(
                intrinsics.FocalLength, intrinsics.PrincipalPoint, intrinsics.SensorResolution, camera.CurrentResolution));
            return true;
        }

        private static RenderTexture NewTexture(int width, int height)
        {
            var texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            { name = "Room blur GPU buffer", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            texture.Create();
            return texture;
        }

        private static void Release(ref RenderTexture texture)
        {
            if (texture == null) return;
            texture.Release(); UnityEngine.Object.Destroy(texture); texture = null;
        }
#endif

        public void Stop()
        {
            mask.SetFloat("_RoomBlurReady", 0);
#if LOCKEDIN_AI
            if (subscribed) Application.onBeforeRender -= BeforeRender;
            subscribed = false;
            active = false;
            previousDepthTexture = null;
            Release(ref ambientScratch); Release(ref ambientFill);
            // Disable synchronously so AI can open its left camera on the next frame.
            for (int eye = 0; eye < 2; eye++)
            {
                if (cameras[eye] != null) cameras[eye].enabled = false;
                cameras[eye] = null;
                Release(ref scratch[eye]); Release(ref blurred[eye]);
            }
            if (ownsDepth && depth != null) depth.enabled = false;
            depth = null;
            if (captureRoot != null) UnityEngine.Object.Destroy(captureRoot);
            captureRoot = null;
#endif
        }

        public void Dispose()
        {
            Stop();
#if LOCKEDIN_AI
            if (filter != null) UnityEngine.Object.Destroy(filter);
#endif
        }
    }
}
