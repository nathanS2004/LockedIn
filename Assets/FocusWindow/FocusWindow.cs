using UnityEngine;
using UnityEngine.InputSystem;

namespace LockedIn
{
    [RequireComponent(typeof(Camera))]
    public sealed class FocusWindow : MonoBehaviour
    {
        [SerializeField] private Shader maskShader;

        private const string RightHand = "<XRController>{RightHand}/";
        private const float MinWindowSize = 0.025f;
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
        private float distance = 0.65f;
        private float dimming = 0.55f;
        private bool firstCornerPlaced;
        private bool windowPlaced;
        private bool dimEnabled;
        private Vector3 planeOrigin;
        private Vector3 planeRight;
        private Vector3 planeUp;
        private Vector3 planeNormal;
        private Vector2 firstCorner;
        private Vector4 bounds;

        private void Awake()
        {
            if (maskShader == null || !maskShader.isSupported)
            {
                Debug.LogError("LockedIn focus mask shader is unavailable.", this);
                enabled = false;
                return;
            }

            // This quad covers both eyes. The shader projects each eye ray onto the focus plane.
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
            menu = new FocusHomeMenu(transform, SelectBackground, BeginBorder);
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
            menu?.Hide();
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
                menu.Toggle(background);
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

            if (!placingBorder && background != BackgroundStyle.Passthrough && toggle.WasPressedThisFrame())
                dimEnabled = !dimEnabled;

            float stick = adjust.ReadValue<Vector2>().y;
            if (Mathf.Abs(stick) > AimDeadZone)
            {
                if (windowPlaced && background == BackgroundStyle.Dimmed)
                    dimming = Mathf.Clamp(dimming + stick * Time.unscaledDeltaTime * 0.5f, 0.15f, 0.85f);
                else if (placingBorder && !firstCornerPlaced)
                    distance = Mathf.Clamp(distance + stick * Time.unscaledDeltaTime * 0.65f, 0.3f, 1.5f);
            }

            if (placingBorder && !firstCornerPlaced)
            {
                planeNormal = transform.forward;
                planeRight = transform.right;
                planeUp = transform.up;
                planeOrigin = transform.position + planeNormal * distance;
            }

            Vector2 aim = default;
            bool hasAim = placingBorder && hasRay && TryGetAim(pointerRay, out aim);
            if (hasAim && pressed)
            {
                if (!firstCornerPlaced)
                {
                    firstCorner = aim;
                    firstCornerPlaced = true;
                }
                else if (Mathf.Abs(firstCorner.x - aim.x) >= MinWindowSize &&
                         Mathf.Abs(firstCorner.y - aim.y) >= MinWindowSize)
                {
                    bounds = GetBounds(firstCorner, aim);
                    placingBorder = false;
                    windowPlaced = true;
                    dimEnabled = background != BackgroundStyle.Passthrough;
                }
            }

            RenderMask(placingBorder && hasAim, aim);
        }

        private void BeginBorder()
        {
            menu.Hide();
            firstCornerPlaced = false;
            windowPlaced = false;
            dimEnabled = false;
            placingBorder = true;
        }

        private void SelectBackground(BackgroundStyle style)
        {
            background = style;
            dimEnabled = style != BackgroundStyle.Passthrough;
        }

        private void RenderMask(bool showPreview, Vector2 aim)
        {
            overlay.enabled = dimEnabled || showPreview;
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

            mask.SetVector("_PlaneOrigin", planeOrigin);
            mask.SetVector("_PlaneNormal", planeNormal);
            mask.SetVector("_PlaneRight", planeRight);
            mask.SetVector("_PlaneUp", planeUp);
            mask.SetColor("_BackgroundColor", color);
            mask.SetFloat("_Dimming", dimEnabled ? (windowPlaced ? opacity : Mathf.Min(opacity, 0.45f)) : 0f);
            mask.SetFloat("_HasWindow", windowPlaced ? 1f : 0f);
            mask.SetFloat("_Preview", showPreview ? 1f : 0f);
            mask.SetFloat("_ShowBounds", showPreview && firstCornerPlaced ? 1f : 0f);
            mask.SetVector("_Aim", new Vector4(aim.x, aim.y, 0f, 0f));
            mask.SetVector("_Window", showPreview && firstCornerPlaced ? GetBounds(firstCorner, aim) : bounds);
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

        private bool TryGetAim(Ray ray, out Vector2 point)
        {
            point = default;
            var plane = new Plane(planeNormal, planeOrigin);
            if (!plane.Raycast(ray, out float hit))
                return false;

            Vector3 offset = ray.GetPoint(hit) - planeOrigin;
            point = new Vector2(Vector3.Dot(offset, planeRight), Vector3.Dot(offset, planeUp));
            return true;
        }

        private static Vector4 GetBounds(Vector2 one, Vector2 two)
        {
            return new Vector4(Mathf.Min(one.x, two.x), Mathf.Min(one.y, two.y),
                               Mathf.Max(one.x, two.x), Mathf.Max(one.y, two.y));
        }
    }
}
