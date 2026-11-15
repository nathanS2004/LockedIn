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

### Follow-up: Meta Core compatibility and overwritten version pin

The next import exposed six Meta Core 85 compiler errors: removed GetInstanceID calls, the int Physics.BakeMesh overload, and implicit SceneHandle-to-int conversion. The manifest also again contained Sentis 2.2.1, consistent with the previously compiled setup callback running before the updated scripts could load.

Applied Tools/repair_unity66_packages.py to embed the installed Meta Core 85 package under Packages/com.meta.xr.sdk.core and patch three source files. Room mesh baking now retains EntityId end to end. Immersive Debugger uses session-local sequential handles for typed EntityId/SceneHandle keys, preserving its existing int interfaces without truncation or hash collisions. This map lasts for the assembly lifetime and can grow with unique inspected objects. Upstream licenses remain in the embedded package. The repair script is idempotent for this exact package version. It also restores the Sentis 2.6.1 pin.

Validation: runtime/editor compilation with Unity analyzers and 13 geometry checks passed. The actual patched InstanceHandle plus a probe for EntityId mesh baking compiled against installed Unity 6.6 assemblies. Full Meta package compilation, inference backend, Android build, and device behavior remain pending Unity import. Restart Unity before rerunning setup to discard the stale compiled setup callback. Do not remove the embedded package until replacing it with a verified upstream version.

### Safe Mode follow-up: editor scene events

Patched the two OVRSceneChangeListener calls to use EditorUtility.EntityIdToObject and event.entityId, resolving the four reported obsolete-API call sites. Updated the repair script to reproduce these changes and operate when Unity has removed the original cached package after embedding it. Both event types and replacement calls compile against installed Unity 6.6 editor assemblies. General runtime/editor compilation also passed; this run's geometry executable was blocked by Windows Application Control, so no new runtime test pass is claimed. Full Unity import remains pending.

### Duplicate helper repair
The repair helper checked for old text before checking whether the replacement already existed. Because the replacement included the original property declaration, rerunning it duplicated DebugIds/GetDebugId. Removed the identical duplicate and changed the helper to check for the completed replacement first. Verified two consecutive repair runs leave every embedded package C# file and manifest byte-identical. Compiled the actual repaired InstanceHandle source with installed Unity assemblies and analyzers successfully. Full Unity refresh remains pending.

### MRUK diagnostic compatibility
Embedded MR Utility Kit 85 and replaced the single GetInstanceID call in MRUKGlobalContext's diagnostic string with GetEntityId. Added this to the reproducible repair script. Compiled the actual source against Unity 6.6 assemblies with only MRUK.UpdateGlobalContext stubbed; full package import and device tests remain pending.

### Building Blocks editor and audio module
Resolved all eight reported errors: migrated CustomHierarchyIcon callbacks and BlocksWindow hierarchy drag/drop to EntityId/V2 APIs, and enabled com.unity.modules.unitywebrequestaudio 1.0.0 for AIBlocks HTTP audio downloads. Updated reproducible repair script. Compiled the complete Meta.XR.BuildingBlocks.Editor and Meta.XR.BuildingBlocks.AIBlocks assemblies using Unity-generated Bee response files with outputs redirected to Temp; both exited 0. Added the missing audio module reference explicitly for the standalone AIBlocks check. Unity still must resolve the manifest/import and perform its full build.

### AI provider editor compatibility
Changed CredentialStorageEditor's per-object test-status string key from GetInstanceID to GetEntityId. No credential values were read or changed. Added the patch to the repair script. Full Meta.XR.Editor.BuildingBlocks.AIBlocks assembly compiled with Unity-generated response settings, exit 0; output redirected to Temp. Unity refresh and full Android build remain pending.

### Setup with embedded dependencies
WorkspaceAISetup now excludes packages with embedded Packages/name/package.json files from Client.AddAndRemove, preserving the patched SDKs. It also requests the required UnityWebRequestAudio module and handles an empty install list without calling UPM. The existing imported-type checks remain in place. Full project editor assembly compiled using Unity's generated response file, exit 0. Setup must be rerun after Unity reloads the script.

### Gradle manifest callback failure
Build reached post-generation but Meta sample metadata callback recursively enumerated generated build/intermediates manifests, then attempted to load a disappeared xrmanifest artifact. Restricted callback to unityLibrary/src/main/AndroidManifest.xml, the source application manifest Gradle merges. Source exists and parses with application node. Compiled complete Oculus.VR.Editor assembly using Unity response settings and original assembly name (required for friend assembly access), exit 0. Added durable repair step. No build cache removed. Full Android build retry pending.

### First-use instructions
Added a four-page local-persistence walkthrough, replayable help, preflight guidance for all four Home actions, and clearer physical placement prompts. See docs/instructions.md for controls, behavior, validation and headset checklist. Runtime/AI/editor compilation passed; device UX verification pending.
