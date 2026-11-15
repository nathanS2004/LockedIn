# LockedIn

LockedIn is a Meta Quest 3 hackathon prototype for reducing visual distractions while studying. The planned mixed-reality experience lets you keep a work area, such as a laptop or desk, visible and reduce distractions around it.

## Run the first Unity scene

1. In Unity Hub, add this repository folder as a project. The project uses Unity `6000.6.9f1`.
2. In the Project window, open `Assets/Scenes/FocusLensDemo.unity`.
3. Open the **Game** tab to see the desk, monitor, and two background objects. The monitor material was updated to bright green as a visible edit test.

Unity 6.6 may display a deprecation warning for the Built-In Render Pipeline used by this test scene.

## LockedIn home and backgrounds

Build and run `Assets/Scenes/LockedInPassthrough.unity` on the Quest. The home screen opens on launch. Use the right controller's trigger to select and its grip to open or close the menu. See [Home screen and backgrounds](docs/devlog/2026-11-14-home-and-backgrounds.md) for the full controls.

## Physical knob

The knob controls photo/color opacity or Room Blur strength while keeping the existing detection and Home UI. The Nano 33 IoT sends directly over Wi-Fi to Quest `192.168.1.171:4210`; no laptop bridge is needed at runtime. Laptop USB can power the Nano while Quest runs unplugged. See [direct Nano setup](Arduino/LockedInKnobWiFi/README.md), [app setup/testing](docs/potentiometer-control.md), and [experimental room blur](docs/room-blur.md).

Room Blur replaces Slate Blue. Valid camera/depth areas use real-image Gaussian blur; a soft camera-derived fill covers the remaining surroundings while the detected workspace stays clear. Peripheral fill is approximate. The five-page tutorial includes the potentiometer.

## Development log

- [Session handoff and verification record](docs/devlog/2026-11-14-potentiometer-session-handoff.md)
- [Room blur and the knob tutorial](docs/devlog/2026-11-14-room-blur-and-knob-tutorial.md)
- [Direct Nano Wi-Fi](docs/devlog/2026-11-14-nano-direct-wifi.md)
- [Full-app integration and build repairs](docs/devlog/2026-11-14-environment-knob-integration.md)

- [2026-11-15 — AI workspace selection prototype](docs/devlog/2026-11-15-ai-workspace-selection.md)

- [2026-11-14 — Unity scene and Quest 3 USB setup](docs/devlog/2026-11-14-unity-scene-and-quest-usb.md)
- [2026-11-14 — First Quest 3 launch](docs/devlog/2026-11-14-first-quest-launch.md)
- [2026-11-14 — Native passthrough prototype](docs/devlog/2026-11-14-native-passthrough.md)
- [2026-11-14 — Adjustable focus window](docs/devlog/2026-11-14-focus-window.md)
- [2026-11-14 — Home screen and backgrounds](docs/devlog/2026-11-14-home-and-backgrounds.md)
- [2026-11-14 — Calibrate the focus border](docs/devlog/2026-11-14-calibrated-border.md)
- [2026-11-14 — One-click screen selection](docs/devlog/2026-11-14-auto-screen-selection.md)

Each milestone PR should document the steps taken, what was verified, and the next step. Record whether validation happened in Unity's Game tab or on the headset.

## AI screen and keyboard selection

Run **LockedIn > Set Up AI Workspace Detection** in Unity once, wait for package imports and compilation, then Build and Run. On Quest choose **Detect Workspace (AI)**, grant camera/scene access, aim at the screen with both screen and keyboard in view, and pull the trigger. **A** confirms the preview, trigger rescans, **B** starts manual placement, and right grip cancels. Separate clear regions remain fixed in the room until the next scan.

This optional prototype uses Meta's pretrained YOLOv9 sample locally on the headset. The model and required packages must be downloaded by the Unity setup command. Headset validation of this new path is pending. See the [implementation and verification log](docs/devlog/2026-11-15-ai-workspace-selection.md) for limitations and reproducible checks.
