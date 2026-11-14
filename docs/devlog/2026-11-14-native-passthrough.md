# Native passthrough prototype

## November 14, 2026

Confirmed that text on the laptop is readable through the Quest 3's system passthrough. Added a separate `LockedInPassthrough` launch scene using the Unity OpenXR Meta camera feature, AR Foundation, and a transparent tracked camera. The virtual desk scene remains available in the project.

## Test the new scene on Quest 3

1. Switch back to Unity and let Package Manager resolve the new Unity OpenXR Meta dependency and its transitive dependencies.
2. In **Edit → Project Settings → XR Plug-in Management → OpenXR**, select Android and confirm **Meta Quest: Camera (Passthrough)** is enabled. Check **Project Validation** before building.
3. Open **File → Build Profiles → Meta Quest**. In **Scene List**, confirm `LockedInPassthrough` is the first enabled scene. If a custom profile overrides the list, add the scene to that list and move it to the top.
4. Select the Quest 3 under **Run Device** and choose **Build and Run**. Test in a stationary position and confirm the real laptop remains visible and its text is readable after the app launches.

This build uses system-composited passthrough. The next implementation step is an adjustable focus area and a before-and-after toggle. The new app build and headset view still need to be verified.

## Scene import fix

Unity's Editor log showed that the scene metadata contained a 33-character GUID, so Unity ignored the file and marked its build-list row **Deleted**. Corrected the GUID to 32 hexadecimal characters and restored the matching enabled row at the top of the build list. Unity resolved OpenXR Meta 2.7.0, AR Foundation 6.6.2, and XR Composition Layers 2.6.1.

If the **Run Device** list stays on **Default device**, close Meta Quest Developer Hub and click **Refresh**. The Unity log showed its Android SDK's ADB server conflicting with Developer Hub's bundled ADB.

## OpenXR build validation fix

Unity imported the corrected passthrough scene, then stopped the build because the Android **Meta Quest: Camera (Passthrough)** feature requires **Meta Quest: Session**. Enabled both features in the Android OpenXR settings. The input polling and Thin Link Time Optimization messages are optional. Rebuild and verify the passthrough scene on the headset.

## Reopen the installed app from Windows

The first APK is installed and its launch activity was resolved over ADB. If **Unknown Sources** is missing, connect the Quest by USB and run the following in Windows PowerShell. This starts the existing build.

```powershell
$adb = 'C:\Program Files\Unity\Hub\Editor\6000.6.9f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe'
& $adb shell am start -n 'com.DefaultCompany.FocusLens_Unity_Demo/com.unity3d.player.UnityPlayerGameActivity'
```
