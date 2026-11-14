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
            mask = new Material(maskShader);
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
            if (overlay != null)
                Destroy(overlay.gameObject);
            if (mask != null)
                Destroy(mask);
            if (mesh != null)
                Destroy(mesh);
        }

        private void LateUpdate()
        {
            if (reset.WasPressedThisFrame())
            {
                firstCornerPlaced = false;
                windowPlaced = false;
                dimEnabled = false;
            }

            if (windowPlaced && toggle.WasPressedThisFrame())
                dimEnabled = !dimEnabled;

            float stick = adjust.ReadValue<Vector2>().y;
            if (Mathf.Abs(stick) > AimDeadZone)
            {
                if (windowPlaced)
                    dimming = Mathf.Clamp(dimming + stick * Time.unscaledDeltaTime * 0.5f, 0.15f, 0.85f);
                else if (!firstCornerPlaced)
                    distance = Mathf.Clamp(distance + stick * Time.unscaledDeltaTime * 0.65f, 0.3f, 1.5f);
            }

            if (!firstCornerPlaced && !windowPlaced)
            {
                planeNormal = transform.forward;
                planeRight = transform.right;
                planeUp = transform.up;
                planeOrigin = transform.position + planeNormal * distance;
            }

            Vector2 aim = default;
            bool hasAim = !windowPlaced && TryGetAim(out aim);
            if (hasAim && place.WasPressedThisFrame())
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
                    windowPlaced = true;
                    dimEnabled = true;
                    hasAim = false;
                }
            }

            bool showPreview = hasAim && !windowPlaced;
            overlay.enabled = dimEnabled || showPreview;
            if (!overlay.enabled)
                return;

            mask.SetVector("_PlaneOrigin", planeOrigin);
            mask.SetVector("_PlaneNormal", planeNormal);
            mask.SetVector("_PlaneRight", planeRight);
            mask.SetVector("_PlaneUp", planeUp);
            mask.SetFloat("_Dimming", dimEnabled ? dimming : 0f);
            mask.SetFloat("_Preview", showPreview ? 1f : 0f);
            mask.SetFloat("_ShowBounds", showPreview && firstCornerPlaced ? 1f : 0f);
            mask.SetVector("_Aim", new Vector4(aim.x, aim.y, 0f, 0f));
            mask.SetVector("_Window", showPreview && firstCornerPlaced ? GetBounds(firstCorner, aim) : bounds);
        }

        private bool TryGetAim(out Vector2 point)
        {
            point = default;
            if (tracked.ReadValue<float>() < 0.5f || pointerPosition.activeControl == null ||
                pointerRotation.activeControl == null)
                return false;

            // The rig's Camera Offset maps the controller's tracking-space pose into the scene.
            Transform trackingSpace = transform.parent;
            Vector3 origin = trackingSpace.TransformPoint(pointerPosition.ReadValue<Vector3>());
            Vector3 direction = trackingSpace.TransformDirection(pointerRotation.ReadValue<Quaternion>() * Vector3.forward);
            var plane = new Plane(planeNormal, planeOrigin);
            var ray = new Ray(origin, direction);
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
