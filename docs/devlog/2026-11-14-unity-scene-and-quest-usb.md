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
| USB debugging and headset detection by Unity | Pending |
| Scene launches with head tracking on the Quest 3 | Pending |
| Live Codex connection to the running Unity Editor | Pending. A batch launch from Codex timed out connecting to Unity's licensing client |

Unity displayed a Built-In Render Pipeline deprecation warning. The original scene still rendered in the Game tab.

## Next milestone

Install **Android Build Support**, **Android SDK & NDK Tools**, and **OpenJDK** for Unity `6000.6.9f1`. Enable Developer Mode and USB debugging on the Quest. Configure the Meta Quest build profile and OpenXR, add a tracked XR Origin to the scene, then use **Build and Run** to verify the scene inside the headset.
