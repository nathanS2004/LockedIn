# Complete app restored; knob controls environment opacity

The user clarified that the complete app's Home UI, backgrounds and detection
must remain available, and the knob must change the selected environment's
transparency. The earlier standalone obscuring prototype did not meet that
intent. The feature branch started at `3d33618`, before those app milestones.

Restored the complete app's files from locally available snapshot `b3eb760`
(the latest auto-screen-selection milestone, including the YOLO milestone),
without merging branches, committing or pushing. Existing Editor XR settings
were retained and augmented with Android depth raycasts and Meta XR Feature.
A snapshot of the pre-integration files is under ignored
`Temp/knob-before-full-app.zip`; Temp is not permanent storage.

## Resulting implementation

- Retained the Python USB bridge and KnobUdpReceiver. The actual launch scene
  references the receiver from the restored Main Camera FocusWindow component.
- FocusWindow now applies normalized knob strength directly to its existing
  `_Dimming` opacity input for photos, solid colors and Dim Room. The original
  45% cap remains before workspace acceptance. Minimum is exactly clear;
  a settled maximum is exactly opaque outside the accepted workspace.
- The original FocusMask shader, YOLO provider, geometry, filtering and depth
  selection sources are unchanged. Screen/keyboard clear volumes are never
  recomputed by knob input. No second rendering system or camera quad is added.
- Existing controller flows remain: A confirms AI review or toggles background
  bypass during focus; B starts manual placement; grip opens/closes Home; trigger
  selects/scans/records points. The knob takes ownership of opacity while enabled.
  Turning off Knob Controls Opacity restores the original controller-only behavior.
- The Home menu and scan/calibration prompts remain usable at zero/no signal.
  The compact status label reports environment opacity, connection and bypass.
- Zero, lost signal, bypass and pause/focus loss clear the environment immediately.
  Reconnection/nonzero changes ease through a 60 ms headset time constant.
- Removed only the first iteration's FocusAreaObscuring script/shaders and its
  independent rectangle. Preserved the working receiver and serial sketch/wiring.

## Assets and dependency restoration

Restored all three licensed panorama JPEGs and the pinned 2,365,288-byte YOLO
model. Model size and Git blob SHA1 match the original setup's expected values.
Downloaded exact Meta Core/MRUK 85 archives with registry hash verification and
embedded them locally, retaining licenses and applying the complete app's existing
Unity 6.6 repairs. The import exposed two additional obsolete SceneDropHandler
registrations; migrated both to Add/RemoveDropHandlerV2 and added those changes
to the idempotent repair script. Full affected Meta editor assembly compilation
then passed with Unity's generated response settings.

`Tools/restore_full_app.py` reproduces local binary/SDK restoration. Large downloads
are ignored rather than added to source control. Inference Engine 2.6.1 and other
dependencies resolve through Unity Package Manager. The lockfile now records the
real resolved packages and embedded Meta SDKs. Android `LOCKEDIN_AI` and Meta XR
Feature are configured in project settings.

Corrected the Internet permission's serialized key to `ForceInternetPermission`
(the earlier lowercase key was not retained by Unity). The Android build callback
also forces this Player Setting and ensures INTERNET is in the merged source
manifest, alongside the existing camera/scene permissions when AI is enabled.

## Validation actually performed

| Check | Result |
| --- | --- |
| Python bridge tests, including real localhost UDP with simulated serial/silence | 6 passed |
| Existing workspace filtering/geometry executable | 13 checks passed |
| Opacity executable: endpoints, disconnect, bypass, smoothing, reconnect, frame rates, finite bounds | 12 checks passed |
| Receiver protocol executable: valid/invalid/version/range/nonfinite/oversized/multiline packets | 15 checks passed |
| Runtime fallback and Android AI runtime compilation against installed Unity and real dependency assemblies, with Unity analyzers | Passed |
| Project editor source compilation | Passed |
| Full Meta Building Blocks editor assembly after V2 repair | Passed, compiler exit 0 |
| Scene local IDs, receiver wiring, shader/geometry/detector unchanged, AI/network settings | Passed |
| Repeated Meta compatibility repair leaves patched sources unchanged | Passed |
| Restoration command with already present assets/SDKs | Passed |
| `git diff --check` | Passed |
| Previous standalone prototype APK launches with status inside Quest | User confirmed earlier in this conversation |
| New full-app APK build/install, shader compilation and on-device knob/detection/stereo behavior | Not verified in this integration turn |

Standalone compile checks report existing Unity serialized-field/fallback-path
warnings and a deprecated inference TextureTransform call; these are not compile
errors. Unity's full import initially logged the two SDK editor errors noted
above; the patched assembly compiles, but the open Editor must refresh/reimport
before the Console can reflect the correction. The standalone verifier now uses
the installed .NET runtime instead of requiring .NET 9 and includes the knob
sources and executable checks.

Follow [the revised setup/test guide](../potentiometer-control.md). Build and Run
again before expecting the restored Home UI on the headset; the previous APK
is still the standalone prototype until replaced. Check AI/manual/depth selection,
then choose a background and test knob minimum/midpoint/maximum, A bypass,
disconnect/reconnect, pause/resume and both-eye clear-region alignment.

## Failed deployment diagnosed from the headset screenshot

The user subsequently saw the old `Obscuring ... | LIVE` label and B/Y placement
controls. The current source scene references FocusWindow and the environment
status UI; that older overlay script is absent. `Logs/Editor.log` identifies the
latest build as failed before packaging: Meta's OVRGradleGeneration rejects Gamma
color space with `Oculus Utilities Plugin with OpenXR only supports linear lighting`.
The old installed APK therefore remained available on the Quest.

Changed `ProjectSettings/ProjectSettings.asset` `m_ActiveColorSpace` from 0 (Gamma)
to 1 (Linear), matching the installed SDK's explicit requirement. Confirmed the
scene references, AI define and Internet permission remain configured. The open
Editor should also show Player > Other Settings > Rendering > Color Space = Linear
before retrying Build And Run. A fresh successful APK install and headset test are
still required; the screenshot alone does not demonstrate the restored app running.

## Android packaging repair and successful installation

The next GUI build reached Gradle but failed with AccessDeniedException in
`packageReleaseResources/merged.dir/values`. Generated resource directories/files
had Windows read-only flags. Cleared only generated Gradle outputs, first in
unityLibrary and then launcher; no source, scene, SDK cache or project Library
deletion was performed. Added `Tools/repair_android_build_cache.py` to reproduce
that narrow repair with resolved-path checks and preservation of other attributes.

Packaging then exposed Meta-generated library manifest references to
`@string/app_name` and `@mipmap/app_icon`. Those resources belong to launcher, so
AGP's independent unityLibrary resource verification failed. The final manifest
callback now removes only those exact library references; launcher retains the
name/icon, consistent with [Android manifest merge priorities](https://developer.android.com/build/manage-manifests).
The callback runs after Meta (int.MaxValue) so camera/scene/Internet permissions
cannot be removed by Meta's subsequent rewrite. Applied equivalent corrections
to the current generated project and reran the installed Unity Gradle toolchain.

Results:

- Gradle `assembleRelease`: **BUILD SUCCESSFUL**, 125 tasks, 11 seconds.
- New APK copied to ignored `Builds/LockedIn-FullApp-Knob.apk` (81,826,167 bytes).
- Verified native generated code contains YoloWorkspaceDetector.
- APK permission inspection confirms INTERNET, HEADSET_CAMERA and USE_SCENE.
- `adb install -r`: **Success**, preserving existing application data.
- ADB cold launch: **Status: ok**; new app process remains present.
- Startup log shows Vulkan and Meta XR initialization; no startup exception was
  seen in the inspected log. App lacked foreground focus during the check, so
  headset UI appearance and detection accuracy are not claimed verified.
- Runtime/AI/editor compilation and 40 geometry/opacity/protocol checks passed
  after the manifest source change.

The complete APK has now actually replaced the old prototype on the connected
Quest. User should dismiss the old Unity failure dialog and open the installed
app in the headset. Grant camera/scene permissions when selecting AI detection.
Unity's previous failed build report/Console remains historical; the successful
manual packaging log is `Logs/gradle-knob-retry.log`. Future Build And Run uses
the corrected source callback. No merge or push was performed.
