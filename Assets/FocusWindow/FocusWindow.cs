using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;

namespace LockedIn
{
    [RequireComponent(typeof(Camera))]
    public sealed class FocusWindow : MonoBehaviour
    {
        [SerializeField] private Shader maskShader;
        [SerializeField] private ARRaycastManager raycasts;

        private const string ScenePermission = "com.oculus.permission.USE_SCENE";
        private ScreenDepthSelection screenSelection;
        private Coroutine screenScan;
        private bool selectingScreen;
        private float nextPreviewSample;
        private float previewDistance = 0.65f;

        private const string RightHand = "<XRController>{RightHand}/";
        private const float ProbeOffset = 0.06f;
        private const float BorderPadding = 0.035f;
        private const float MinScreenEdge = 0.06f;
        private const float AimDeadZone = 0.15f;
        private const float OverlayDistance = 0.1f;

        private readonly InputAction pointerPosition = new InputAction(binding: RightHand + "pointerPosition");
        private readonly InputAction pointerRotation = new InputAction(binding: RightHand + "pointerRotation");
        private readonly InputAction tracked = new InputAction(binding: RightHand + "isTracked");
        private readonly InputAction place = new InputAction(type: InputActionType.Button, binding: RightHand + "triggerPressed");
        private readonly InputAction toggle = new InputAction(type: InputActionType.Button, binding: RightHand + "primaryButton");
        private readonly InputAction reset = new InputAction(type: InputActionType.Button, binding: RightHand + "secondaryButton");
        private readonly InputAction adjust = new InputAction(binding: RightHand + "thumbstick");

        private readonly InputAction openMenu = new InputAction(type: InputActionType.Button, binding: RightHand + "grip");
        private FocusHomeMenu menu;
        private BackgroundStyle background = BackgroundStyle.Dimmed;
        private bool placingBorder;

        private Material mask;
        private MeshRenderer overlay;
        private Mesh mesh;
        private float dimming = 0.55f;
        private bool windowPlaced;
        private bool dimEnabled;
        private int calibrationStep;
        private Vector3 screenOrigin;
        private Vector3 screenRight;
        private Vector3 screenUp;
        private Vector3 screenNormal;
        private Vector3 windowMin;
        private Vector3 windowMax;

        private void Awake()
        {
            if (maskShader == null || !maskShader.isSupported)
            {
                Debug.LogError("LockedIn focus mask shader is unavailable.", this);
                enabled = false;
                return;
            }

            // This quad covers both eyes. The shader tests each eye ray against the calibrated volume.
            var surface = new GameObject("Focus mask");
            surface.transform.SetParent(transform, false);
            surface.transform.localPosition = Vector3.forward * OverlayDistance;
            mesh = new Mesh
            {
                name = "Focus mask quad",
                vertices = new[]
                {
                    new Vector3(-2, -2, 0), new Vector3(2, -2, 0),
                    new Vector3(2, 2, 0), new Vector3(-2, 2, 0)
                },
                triangles = new[] { 0, 1, 2, 0, 2, 3 }
            };
            mesh.RecalculateBounds();
            surface.AddComponent<MeshFilter>().sharedMesh = mesh;
            overlay = surface.AddComponent<MeshRenderer>();
            overlay.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            overlay.receiveShadows = false;
            mask = new Material(maskShader) { renderQueue = 2990 };
            screenSelection = new ScreenDepthSelection(raycasts);
            menu = new FocusHomeMenu(transform, SelectBackground, BeginBorder, BeginAutoSelect);
            overlay.sharedMaterial = mask;
            overlay.enabled = false;
        }

        private void OnEnable()
        {
            pointerPosition.Enable();
            pointerRotation.Enable();
            tracked.Enable();
            place.Enable();
            toggle.Enable();
            reset.Enable();
            adjust.Enable();
            openMenu.Enable();
        }

        private void OnDisable()
        {
            pointerPosition.Disable();
            pointerRotation.Disable();
            tracked.Disable();
            place.Disable();
            toggle.Disable();
            reset.Disable();
            adjust.Disable();
            openMenu.Disable();
            CancelAutoSelection();
            if (raycasts != null)
                raycasts.enabled = false;
            menu?.Hide();
            menu?.SetCalibrationVisible(false);
            if (overlay != null)
                overlay.enabled = false;
        }

        private void OnDestroy()
        {
            pointerPosition.Dispose();
            pointerRotation.Dispose();
            tracked.Dispose();
            place.Dispose();
            toggle.Dispose();
            reset.Dispose();
            adjust.Dispose();
            openMenu.Dispose();
            menu?.Dispose();
            if (overlay != null)
                Destroy(overlay.gameObject);
            if (mask != null)
                Destroy(mask);
            if (mesh != null)
                Destroy(mesh);
        }

        private void LateUpdate()
        {
            if (openMenu.WasPressedThisFrame())
            {
                if (selectingScreen)
                    CancelAutoSelection();
                menu.Toggle(background);
                menu.SetCalibrationVisible(placingBorder);
                RenderMask(false, default);
                return;
            }

            bool hasRay = TryGetPointerRay(out Ray pointerRay);
            bool pressed = place.WasPressedThisFrame();
            if (menu.IsOpen)
            {
                menu.UpdatePointer(hasRay, pointerRay, pressed, background);
                RenderMask(false, default);
                return;
            }

            if (reset.WasPressedThisFrame())
                BeginBorder();

            if (selectingScreen)
            {
                UpdateAutoSelect(hasRay, pointerRay, pressed);
                return;
            }

            if (placingBorder)
            {
                bool showProbe = hasRay;
                Vector3 probe = pointerRay.GetPoint(ProbeOffset);
                if (calibrationStep == 3 && toggle.WasPressedThisFrame())
                    FinishBorder();
                else if (hasRay && pressed)
                    RecordCalibrationPoint(probe);

                RenderMask(placingBorder && showProbe, probe);
                return;
            }

            if (background != BackgroundStyle.Passthrough && toggle.WasPressedThisFrame())
                dimEnabled = !dimEnabled;

            float stick = adjust.ReadValue<Vector2>().y;
            if (windowPlaced && background == BackgroundStyle.Dimmed && Mathf.Abs(stick) > AimDeadZone)
                dimming = Mathf.Clamp(dimming + stick * Time.unscaledDeltaTime * 0.5f, 0.15f, 0.85f);

            RenderMask(false, default);
        }

        private void BeginBorder()
        {
            CancelAutoSelection();
            menu.Hide();
            calibrationStep = 0;
            windowPlaced = false;
            dimEnabled = false;
            placingBorder = true;
            menu.ShowCalibration("1 / 3   Top-left of the screen", "Line up the cyan dot with the corner and pull the trigger.");
        }

        private void BeginAutoSelect()
        {
            menu.Hide();
            placingBorder = false;
            selectingScreen = true;
            nextPreviewSample = 0f;
            previewDistance = 0.65f;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(ScenePermission))
            {
                menu.ShowCalibration("Allow scene access", "Screen selection needs depth access. Press B for manual placement.");
                UnityEngine.Android.Permission.RequestUserPermission(ScenePermission);
                return;
            }
#endif
            ShowAimPrompt();
        }

        private void ShowAimPrompt()
        {
            menu.ShowCalibration("Auto Select Screen", "Aim at the center of the screen and pull the trigger once. B for manual.");
        }

        private void UpdateAutoSelect(bool hasRay, Ray ray, bool pressed)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(ScenePermission))
            {
                RenderMask(false, default, true);
                return;
            }
#endif
            if (raycasts == null)
            {
                menu.ShowCalibration("Depth unavailable", "Press B to place a border manually.");
                RenderMask(false, default, true);
                return;
            }

            if (!raycasts.enabled)
            {
                raycasts.enabled = true;
                ShowAimPrompt();
            }

            if (screenScan == null && hasRay && Time.unscaledTime >= nextPreviewSample)
            {
                nextPreviewSample = Time.unscaledTime + 0.10f;
                if (screenSelection.TryDepthPoint(ray, out Vector3 surface))
                    previewDistance = Mathf.Clamp(Vector3.Distance(ray.origin, surface), 0.15f, 2f);
            }

            if (screenScan == null && hasRay && pressed)
            {
                menu.ShowCalibration("Checking screen edges", "Hold still for a moment.");
                screenScan = StartCoroutine(screenSelection.FindScreen(ray, transform.position, transform.right,
                                                                       ApplyAutoSelection));
            }

            RenderMask(hasRay && screenScan == null, ray.GetPoint(previewDistance), true);
        }

        private void ApplyAutoSelection(ScreenDepthSelection.ScreenVolume? result)
        {
            screenScan = null;
            if (!result.HasValue)
            {
                menu.ShowCalibration("Screen edges unclear", "Aim at the screen center and try again, or press B for manual.");
                return;
            }

            ScreenDepthSelection.ScreenVolume volume = result.Value;
            screenOrigin = volume.Origin;
            screenRight = volume.Right;
            screenUp = volume.Up;
            screenNormal = volume.Normal;
            windowMin = volume.Min;
            windowMax = volume.Max;
            selectingScreen = false;
            FinishBorder();
        }

        private void CancelAutoSelection()
        {
            if (!selectingScreen)
                return;

            if (screenScan != null)
                StopCoroutine(screenScan);
            screenScan = null;
            selectingScreen = false;
            menu?.HideCalibration();
        }

        private void RecordCalibrationPoint(Vector3 point)
        {
            switch (calibrationStep)
            {
                case 0:
                    screenOrigin = point;
                    calibrationStep = 1;
                    menu.ShowCalibration("2 / 3   Top-right of the screen", "Line up the dot with the next corner and pull the trigger.");
                    break;
                case 1:
                    Vector3 topEdge = point - screenOrigin;
                    float width = topEdge.magnitude;
                    if (width < MinScreenEdge)
                    {
                        menu.ShowCalibration("2 / 3   Top-right of the screen", "Move farther from the first corner and try again.");
                        return;
                    }

                    screenRight = topEdge / width;
                    windowMax.x = width + BorderPadding;
                    calibrationStep = 2;
                    menu.ShowCalibration("3 / 3   Bottom-left of the screen", "Line up the dot with the next corner and pull the trigger.");
                    break;
                case 2:
                    Vector3 side = Vector3.ProjectOnPlane(point - screenOrigin, screenRight);
                    float height = side.magnitude;
                    if (height < MinScreenEdge)
                    {
                        menu.ShowCalibration("3 / 3   Bottom-left of the screen", "Move below the top edge and try again.");
                        return;
                    }

                    screenUp = -side / height;
                    screenNormal = Vector3.Cross(screenRight, screenUp).normalized;
                    windowMin = new Vector3(-BorderPadding, -height - BorderPadding, -BorderPadding);
                    windowMax.y = BorderPadding;
                    windowMax.z = BorderPadding;
                    calibrationStep = 3;
                    menu.ShowCalibration("Include the keyboard?", "Trigger at the front of the keyboard, or press A for screen only.");
                    break;
                case 3:
                    Vector3 delta = point - screenOrigin;
                    var local = new Vector3(Vector3.Dot(delta, screenRight), Vector3.Dot(delta, screenUp),
                                            Vector3.Dot(delta, screenNormal));
                    windowMin = Vector3.Min(windowMin, local - Vector3.one * BorderPadding);
                    windowMax = Vector3.Max(windowMax, local + Vector3.one * BorderPadding);
                    FinishBorder();
                    break;
            }
        }

        private void FinishBorder()
        {
            placingBorder = false;
            windowPlaced = true;
            dimEnabled = background != BackgroundStyle.Passthrough;
            menu.HideCalibration();
        }

        private void SelectBackground(BackgroundStyle style)
        {
            background = style;
            dimEnabled = style != BackgroundStyle.Passthrough;
        }

        private void RenderMask(bool showPreview, Vector3 probe, bool suppressBackground = false)
        {
            bool showBackground = dimEnabled && !suppressBackground;
            overlay.enabled = showBackground || showPreview;
            if (!overlay.enabled)
                return;

            Color color = Color.black;
            float opacity = dimming;
            switch (background)
            {
                case BackgroundStyle.Slate:
                    color = new Color32(35, 55, 82, 255);
                    opacity = 1f;
                    break;
                case BackgroundStyle.Forest:
                    color = new Color32(28, 64, 52, 255);
                    opacity = 1f;
                    break;
            }

            mask.SetVector("_WindowOrigin", screenOrigin);
            mask.SetVector("_WindowRight", screenRight);
            mask.SetVector("_WindowUp", screenUp);
            mask.SetVector("_WindowNormal", screenNormal);
            mask.SetVector("_WindowMin", windowMin);
            mask.SetVector("_WindowMax", windowMax);
            mask.SetColor("_BackgroundColor", color);
            mask.SetFloat("_Dimming", showBackground ? (windowPlaced ? opacity : Mathf.Min(opacity, 0.45f)) : 0f);
            mask.SetFloat("_HasWindow", windowPlaced ? 1f : 0f);
            mask.SetFloat("_Preview", showPreview ? 1f : 0f);
            mask.SetVector("_Probe", probe);
        }

        private bool TryGetPointerRay(out Ray ray)
        {
            ray = default;
            if (tracked.ReadValue<float>() < 0.5f || pointerPosition.activeControl == null ||
                pointerRotation.activeControl == null)
                return false;

            Transform trackingSpace = transform.parent;
            Vector3 origin = trackingSpace.TransformPoint(pointerPosition.ReadValue<Vector3>());
            Vector3 direction = trackingSpace.TransformDirection(pointerRotation.ReadValue<Quaternion>() * Vector3.forward);
            ray = new Ray(origin, direction);
            return true;
        }
    }
}
