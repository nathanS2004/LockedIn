using UnityEngine;

namespace LockedIn
{
    public sealed partial class FocusWindow
    {
        [Header("Physical environment opacity control")]
        [SerializeField] private KnobUdpReceiver knobReceiver;
        [Tooltip("When enabled, the physical knob controls all background opacities. Missing signal restores the room. Disable for the original controller-only behavior.")]
        [SerializeField] private bool knobControlsOpacity = true;
        [Min(0.001f)] [SerializeField] private float knobSmoothingSeconds = 0.06f;

        private readonly EnvironmentOpacityControl knobOpacity = new EnvironmentOpacityControl();
        private float nextKnobStatus;
        private bool opacityPaused, opacityUnfocused;
        private bool opacitySuspended => opacityPaused || opacityUnfocused;

        private float GetEnvironmentOpacity(float originalOpacity, bool showBackground)
        {
            float opacity = 0;
            if (!knobControlsOpacity)
            {
                knobOpacity.Clear();
                opacity = !opacitySuspended && showBackground ? originalOpacity : 0;
            }
            else
            {
                bool connected = knobReceiver != null && knobReceiver.Connected;
                opacity = knobOpacity.Update(connected ? knobReceiver.Value : 0, connected,
                    showBackground && !opacitySuspended, Time.unscaledDeltaTime, knobSmoothingSeconds);
            }

            // Preserve the existing uncalibrated preview cap; accepted workspace bounds never change.
            if (!windowPlaced) opacity = background == BackgroundStyle.RoomBlur ? 0 : Mathf.Min(opacity, 0.45f);
            if (Time.unscaledTime >= nextKnobStatus)
            {
                string state = !knobControlsOpacity ? "CONTROLLER MODE" :
                    knobReceiver == null ? "NO RECEIVER - CLEAR" : knobReceiver.Status;
                string mode = background == BackgroundStyle.RoomBlur ? "Room blur" : "Environment";
                string detail = background == BackgroundStyle.RoomBlur ? "\n" +
                    (!windowPlaced ? "Select a workspace first" : roomBlur.Status) : "";
                menu?.SetFocusState(opacity, mode, state,
                    windowPlaced && !dimEnabled && background != BackgroundStyle.Passthrough, detail, windowPlaced);
                nextKnobStatus = Time.unscaledTime + 0.2f;
            }
            return opacity;
        }

        private void SuspendEnvironment(bool suspended)
        {
            opacityPaused = suspended;
            ClearEnvironment();
        }

        private void ClearEnvironment()
        {
            knobOpacity.Clear();
            roomBlur?.Stop();
            if (mask != null) mask.SetFloat("_Dimming", 0);
            if (opacitySuspended && overlay != null) overlay.enabled = false;
        }

        private void OnApplicationFocus(bool focused)
        {
            opacityUnfocused = !focused;
            ClearEnvironment();
        }
    }
}
