# One-click screen selection

## Try it on Quest 3

1. Refresh the Unity assets and **Build and Run** with `LockedInPassthrough` first.
2. Open **Auto Select Screen** and grant scene access if prompted.
3. Aim the right controller at the center of a stationary laptop screen or monitor. Pull the trigger once and hold still while the app checks the edges.
4. Lean forward and sideways. The fitted region should keep the screen clear. If the scan cannot fit the edges, try the center again or press **B** for manual calibration.

## Implementation

The selector uses AR Foundation's Android OpenXR depth raycasts. It fits a near-vertical surface around the point the user aimed at, then checks that the surface ends at plausible screen edges. It feeds the fitted volume into the same stereo mask as manual calibration. The selected volume stays fixed in the room and applies the active background. Use **Dim Room**, **Slate Blue**, or **Forest Green** to see the effect.

This prototype uses geometry and does not run an image classifier. A screen flush with a wall, a reflective screen, or a similarly shaped flat object may defeat or fool the detector. Auto Select fits the screen only. Manual calibration remains available for the keyboard and for objects the depth scan cannot fit.

## Verification

The C# and scene configuration are checked locally. Live depth access, device permissions, and edge fitting must be tested on the Quest.
