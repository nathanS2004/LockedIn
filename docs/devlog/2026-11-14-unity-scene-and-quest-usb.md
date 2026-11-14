# November 14, 2026 — Unity scene and Quest 3 USB setup

## What we did

1. Opened the FocusLens project in Unity `6000.6.9f1`.
2. Loaded `Assets/Scenes/FocusLensDemo.unity` and viewed it in the Editor's **Game** tab. The scene contains a desk, monitor, background objects, and a fixed camera.
3. Connected the Meta Quest 3 to the Windows laptop over USB.
4. Used Codex to change the monitor material from blue to bright green in `Assets/Materials/Screen.mat`. Checked that the scene's **Monitor Display** object references that material.

## Verification

| Check | Result |
| --- | --- |
| Original scene renders in Unity's Game tab | Confirmed in the Editor |
| Quest 3 plugged into the laptop over USB | Confirmed physical connection |
| Green monitor material assigned to the scene | Verified in the project files. Visual recheck pending |
| Quest 3 detection and debugging authorization through Windows ADB | Confirmed. ADB lists `Quest_3` with status `device` |
| Scene launches with head tracking on the Quest 3 | Pending |
| Live Codex connection to the running Unity Editor | Pending. A batch launch from Codex timed out connecting to Unity's licensing client |

Unity displayed a Built-In Render Pipeline deprecation warning. The original scene still rendered in the Game tab.

## Headset deployment on this laptop

The Windows development laptop uses Intel Arc 140V graphics. Meta Horizon Link does not support this GPU. Use Unity's **Build and Run** over a data-capable USB cable so the Quest 3 runs the APK directly. [Meta's Arc 140V compatibility note](https://developers.meta.com/vr/support/investigations/1131782022535905/)

## Android tools installed

Verified that Unity `6000.6.9f1` has Android Build Support, OpenJDK, the Android NDK, SDK Platform 36, and ADB. Unity Hub reported exit code 1 for the optional Visual Studio Community 2026 installation. The installer log said Visual Studio Community 2026 was already installed, and its executable was present. The Android modules installed independently.

## Headset account and pairing

The Quest 3 was already paired to its primary Meta account. Adding a secondary profile allowed sign-in on the headset, but the Meta Horizon phone app still reported that the headset was already paired. The next step is for the primary account holder to enable Developer Mode through their paired app. USB debugging authorization and device detection still need to be verified. [Meta's guidance on primary and secondary Quest accounts](https://communityforums.atmeta.com/discussions/OtherTroubleshooting/meta-quest-3-initial-setup-and-multiple-family-accounts/1135028/replies/1136153)

## Developer Mode setup

Created a Meta developer team and confirmed that the developer account shows as verified. The Meta Horizon phone app still redirects to developer signup when opening Developer Mode, and the switch is unavailable. The next check is to confirm the Developer Dashboard, phone app, and Quest primary profile use the same account, then sign out and back into the phone app. Developer Mode and USB deployment are pending.

The signup redirect persisted after signing out and back in and reinstalling the phone app. Connecting over USB and trying the Windows Mixed Reality Link shortcut did not display a QR code. That shortcut opens Windows desktop pairing. The next setup attempt is Meta VR Developer Hub's **Device Manager → Set Up New Device** flow over Bluetooth, followed by USB debugging authorization. [Meta VR Developer Hub setup guide](https://developers.meta.com/vr/documentation/unity/ts-mqdh-device-setup/)

Meta VR Developer Hub discovered the Quest 3, then stopped at its account pairing check. The local Developer Hub log recorded `ACCOUNT_MISMATCH`. Compare the primary headset account with the account signed into Developer Hub and the Developer Dashboard. If they match and the error persists, report both the mobile signup redirect and the desktop pairing error to Meta Developer Support.

## Headset connection verified

Windows ADB lists the Quest 3 as `device`, confirming the headset is reachable and debugging is authorized. The Unity project still has a fixed camera and no XR packages. The first headset build has yet to run.

## Next milestone

Enable and switch to the Meta Quest build platform in Unity. Enable OpenXR for Android, add an XR Origin with a tracked camera to the scene, then select the Quest 3 as the Run Device and use **Build and Run**. Verify that the app launches and the view follows head movement.
