# Experimental real-room blur

Room Blur replaces Slate Blue in Choose Background. It filters **actual camera
pixels**, unlike a dim overlay or a feathered clear boundary. The physical knob
changes Gaussian blur radius; other photo/color backgrounds keep their existing
opacity control. Workspace detection, confirmation and clear-volume geometry stay
unchanged. The tutorial now has five pages, with a dedicated physical-knob page.

This is an **experimental camera blur with full surrounding soft fill**. It does not provide the
headset compositor's fully corrected, full-field passthrough pixels to Unity.
Valid camera/depth pixels use reprojected RGB blur, blending into a heavily
softened camera-derived color field in the periphery and at rejected projection
pixels. The user confirmed the first version blurred only a square, then chose
this surrounding-fill approach. The fill approximates unseen surroundings;
it is not true filtering of unseen compositor pixels. The workspace stays clear.

## Rendering and dependencies

Uses the already installed Meta Core/MRUK 85 camera/depth APIs, Unity 6000.6.9f1
Built-In rendering, and the active single-pass instanced/multiview XR mode.
Android Meta Quest Occlusion is enabled in addition to the existing Camera,
Session, Raycasts and Meta XR Feature. Requires the same camera and scene runtime
permissions as AI selection. No SDK download, replacement XR rig, flat camera
quad, CPU image readback, recording or network camera upload is added.

Two PassthroughCameraAccess instances select left/right cameras at 1280x960,
30 Hz. Capture timestamps, camera poses and cropped sensor intrinsics are used
independently for each eye. Frames older than 200 ms or more than 35 ms apart
are rejected; this is a timestamp tolerance, **not guaranteed hardware-synchronized
stereo capture**. Moving objects/capture latency can still cause mismatches and
must be evaluated in the headset.

Each eye's RGB image is downsampled to approximately 320x240 and filtered by
horizontal/vertical nine-tap Gaussian passes. Four small persistent buffers are
reused (about 1.2 MB at 4:3), plus two 16x12 fill buffers; native stereo capture/depth have additional SDK memory
cost. Blur reruns for new camera frames or changing strength, rather than
unconditionally repeating it at display rate. No per-frame string property-name
construction or camera image allocations are needed.

Before rendering, after EnvironmentDepthManager publishes its matrices, the
shader intersects each eye's view ray with environment depth by iterative
projection/unprojection. It maps the resulting world point into the corresponding
capture-time RGB camera. Invalid projection/depth, near/far range rejection,
large reprojection residuals and insufficient source coverage select the shared
ambient fill. Both eyes use the same very low-detail spherical color field;
camera edges fade broadly into it to remove the rectangular alpha cutoff.
Missing/stale complete camera/depth input, tracking loss, bypass and disconnect
still clear the effect. The original screen and
keyboard masks are applied to the result, preserving their clear interiors.

Graphics.Blit render target and sRGB state are restored after the GPU passes.
Capture cameras and owned depth are stopped for menus/calibration, scanning,
bypass, zero input, disconnect and pause; AI selection can then use its existing
left camera without competing with the blur. A subsequent focus session restarts
capture. Unsupported XR modes fail clear.

Native GPU reprojection, occlusion boundaries, lens calibration, source orientation,
dynamic objects, image timing and frame time require on-device evaluation. The
implemented geometry and fallback rules do not establish perceptual alignment or
comfort by themselves. Unsupported pixels use approximate fill rather than
claiming correct camera reconstruction everywhere.

Official references: [Meta camera overview](https://developers.meta.com/vr/documentation/unity/unity-pca-overview/),
[camera integration](https://developers.meta.com/vr/documentation/unity/unity-pca-documentation/),
[depth-supported passthrough occlusion](https://developers.meta.com/vr/documentation/unity/unity-customize-passthrough-passthrough-occlusions/).
The implementation also follows the installed SDK's WorldToViewportPoint crop
calculation and EnvironmentDepthManager's published world/depth matrices.

## Quest acceptance checklist

1. Rebuild/install the updated APK. Follow the five-page tutorial, or replay it
   from Home > How to use LockedIn. The dedicated knob slide is page 4 of 5.
2. Confirm workspace selection still works with AI, depth-only or manual mode.
   Choose Room Blur, close Home, grant access, and turn the knob up slightly.
3. Check that the effect is camera-image blur, rather than black dimming: real
   room colors remain while fine background details soften. Confirm zero removes
   the effect completely and maximum increases the blur radius.
4. Check left/right eyes separately, head translation, head rotation, tilted
   workspace, nearby objects and moving objects. Look for doubled edges, inverted
   images, depth popping and discontinuities at coverage edges. If mismatched,
   use A to bypass; reprojection still needs tuning rather than treating the
   mode as validated.
5. Confirm screen and keyboard stay unfiltered, including their boundary. Look
   away from the workspace and verify there is no stretched flat room image.
   Periphery should have soft fill instead of a clear rectangular boundary;
   its colors approximate the peripheral room.
6. Open Home and rescan with AI after using blur; verify the left camera is released.
   Change to a photo/color and confirm the knob still controls its opacity.
7. At high blur, test A bypass, unplug Nano power, interrupt Wi-Fi, pause/resume,
   permission denial, tracking loss and reconnection. Every missing-data case
   must remain/revert clear. No camera data is sent through the Nano.
8. Measure GPU/CPU frame time and memory in a development APK on Quest. Stereo
   capture, depth fetches and filtering add cost; no performance pass is claimed
   from desktop source compilation.

## Local checks

`python Tools/verify_workspace.py` compiles fallback, Android AI and editor code
against actual installed assemblies and runs geometry, opacity, protocol and
camera-projection tests (53 checks). The projection tests cover centered crop,
resolutions/aspect ratios, principal point, focal scale and separate eye offsets.

`python Tools/verify_blur_shaders.py` checks six D3D vertex/fragment compilations
using Unity's real shader headers, including mono and instanced stereo. It caught
and fixed an HLSL reserved-word issue during implementation. These checks do not
validate Unity's Vulkan/mobile variants, the GPU image, timing or stereo alignment.

The subsequent Unity Android Build And Run succeeded (204 seconds) and installed
the updated APK on Quest. This verifies Android packaging/compilation, including
the shader build, but not its GPU appearance, stereo alignment or frame time.
The physical-headset checklist above remains required.

The user subsequently confirmed visible camera blur but reported its square
coverage boundary. The surrounding-fill follow-up passes desktop shader checks
and C# compilation; its updated APK/device appearance is not yet verified.
