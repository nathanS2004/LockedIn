# AI workspace selection prototype

The user confirmed that the depth-only Auto Select Screen feature worked well on Quest 3. It did not include the keyboard, which is a separate surface. This milestone adds an optional pretrained YOLOv9 path for recognizing the workspace, previewing separate screen and keyboard borders, and confirming their positions for the current session.

## One-time Unity setup

1. Exit Play Mode, return to Unity, and let the scripts import. Use Assets > Refresh if the new menu is missing.
2. Choose **LockedIn > Set Up AI Workspace Detection** in Unity's top menu.
3. The setup downloads Meta's 2.4 MB pretrained model, verifies its exact Git blob digest, and installs Meta MR Utility Kit 85.0.0, Sentis (Unity Inference Engine) 2.6.1, and Unity's built-in AssetBundle module through Package Manager. Sentis 2.6.1 is the released version for Unity 6.6; the sample's older 2.2.1 pin caused a compiler compatibility error. It enables the Meta XR OpenXR feature and the `LOCKEDIN_AI` Android compilation symbol. Wait for compilation and the **LockedIn AI setup complete** Console message.
4. Build and Run with `Assets/Scenes/LockedInPassthrough.unity` first in the scene list. The existing XR Origin and Unity passthrough/raycast components remain in use.

The initial implementation session could not download the dependencies directly, so the setup command was provided for the developer to run in Unity. The developer subsequently ran it: the model downloaded correctly, but package compilation exposed the Unity 6.6 compatibility errors documented below. Until setup completes successfully, the app explains that AI setup is needed; manual calibration and depth-only selection remain available once the project compiles.

## In the headset

1. Choose **Detect Workspace (AI)**. Grant headset-camera and scene access if prompted.
2. Look at the screen and keyboard together. Aim the right controller at the screen and pull the trigger once.
3. Hold your head still during the scan. The app rejects a stale capture if your head moves too far while inference or screen fitting runs.
4. Inspect the cyan candidate borders. The prompt reports **Screen + keyboard found**, **Screen + estimated laptop base**, or **Screen found; keyboard not found**.
5. Press **A** to accept. Pull the trigger to scan again, press **B** for manual placement, or use right grip to cancel and return to the menu.
6. Choose a background to see both clear regions. Lean forward and sideways to check the alignment. Moving the laptop physically requires another scan.

## Implementation

- Camera frames are processed on the headset. No frame upload, AWS, runtime download, or cloud inference is used.
- The pinned Meta sample produces corner boxes, COCO class IDs, and confidence scores. We retain TV/monitor (62), laptop (63), and keyboard (66), then suppress duplicate screens without discarding keyboards nested inside laptop boxes.
- A captured camera pose is transformed through the existing XR tracking space. The user-tested depth edge scan refines the display. Depth consensus fits an upright screen and a horizontal keyboard separately. Implausible dimensions, insufficient depth, and unclear surfaces are rejected.
- If a laptop detection contains its base but no keyboard class is returned, the lower part of its box is tested for a horizontal surface. The preview explicitly labels this as an **estimated laptop base**. A keyboard is never silently fabricated when depth fitting fails.
- The stereo shader takes the union of two clear volumes. The preview is separate from the applied selection, so cancel/failure preserves the previous workspace. A confirms both volumes together.
- The camera, inference worker, and temporary tensors are released after a scan. Inference is scheduled across frames and readback is polled. A new scan creates a new worker, so initial startup latency remains a profiling target.
- The Android build hook adds the camera and scene permissions to the generated manifest, verifies the pinned model, and checks Meta XR Feature when AI is enabled.

## Verification and remaining checks

Passed locally: fallback runtime C# compilation with Android permission paths enabled, Unity Editor setup/build-hook compilation, and 13 geometry/filter checks against installed Unity assemblies. Checks cover nested keyboards, duplicate screen suppression, weak/irrelevant/NaN detections, upright versus horizontal plane fitting, insufficient depth, and one outlying depth sample.

Run `python Tools/verify_workspace.py` from the project root to reproduce these checks. The script now includes Unity's compatibility analyzers and treats UAC0005 as an error. It also compiles the optional YOLO backend when Unity has produced the real Meta/Sentis dependency assemblies; otherwise it explicitly reports that check as skipped. It does not validate the shader on the headset.

Still required: successful Package Manager import, optional-backend compilation, shader compilation, Android build, camera permissions/pose alignment, and live headset accuracy/performance. Test screen-only results, external and built-in keyboards, denied permission, cancellation, rescanning, and regression of manual/Auto Select Screen modes. This is a prototype with rectangular volumes, not pixel-accurate segmentation or tracking of moving devices. World positions are session-local, not saved spatial anchors across launches.

## Model provenance

- Source: https://github.com/oculus-samples/Unity-PassthroughCameraApiSamples
- Pinned commit: `3baac1a1a53aa00af3f639425914f14238a97f84`
- Model: `Assets/PassthroughCameraApiSamples/MultiObjectDetection/SentisInference/Model/yolov9sentis.sentis`
- Bytes: `2365288`; Git blob SHA-1: `926a4f3b79930c69d57d333dc767c54bcc7c625a`
- Local downloaded asset: `Assets/Resources/LockedIn/yolov9sentis.sentis` (recreated by setup; not committed).
- Upstream copyright/license notice: `ThirdPartyNotices/Meta-PassthroughCameraApiSamples-LICENSE.txt`.
- References: https://developers.meta.com/vr/documentation/unity/unity-sample-camera-object-detection/ and https://developers.meta.com/vr/documentation/unity/unity-pca-documentation/.

## Unity 6.6 compatibility repair — November 14, Phoenix time

The user's Unity Console and Editor log showed `UAC0005` in Inference Engine 2.2.1's `PluginInterfaces.cs` and three `CS1069` errors for `UnityEngine.AssetBundle` in Meta's SDK. The original setup used an old sample dependency pin and omitted a built-in engine module needed by Meta's SDK.

Changes:

- Updated the project manifest and setup command to Sentis 2.6.1. Its package source uses `CurrentAssemblies.GetLoadedAssemblies()` on modern Unity instead of the incompatible `AppDomain.GetAssemblies()` call.
- Added `com.unity.modules.assetbundle` 1.0.0 to the manifest and setup command.
- Updated our setup script to use Unity's current assembly enumeration API too.
- Added Unity's actual API-compatibility analyzers to the local verifier and confirmed that a probe using the old API triggers UAC0005. The corrected script compiles without that diagnostic.
- Verified the downloaded 2,365,288-byte model against its pinned Git blob digest.

The shared runtime/editor checks and 13 geometry/filter checks pass. The Unity Package Manager reimport, optional-backend compilation, shader compilation, and Android/headset test must finish before claiming end-to-end success. Return to Unity, refresh, let it resolve the updated manifest, rerun the LockedIn setup command if needed, and then Build and Run.

Unity version reference: https://docs.unity.com/en-us/engine/6000.6/manual/packages-list/packages-all/pack-safe/com-unity-ai-inference
