# Potentiometer control implementation

**Historical first iteration, superseded:** this log describes the standalone
obscuring prototype. The active scene now restores the complete app and controls
its existing environment opacity. See [the integration log](2026-11-14-environment-knob-integration.md).

Implemented on `feature/potentiometer-control`; no merge or push.

Added a Windows Python USB serial bridge with calibration, time-based smoothing, bounded serial draining, UDP rate limiting and no stale-value heartbeat. Added a background-thread Unity UDP receiver with invariant protocol validation and watchdog. Wired it and the obscuring controller into the actual `LockedInPassthrough` launch scene. Preserved the existing XR rig, transparent AR camera and original virtual demo scene. Enabled Android Internet permission and framebuffer alpha preservation.

The visual is accurately named dimming/obscuring, not blur. A stereo-aware shader draws black outside a room-fixed rectangular aperture, with an outward feather that leaves its interior clear. Controller controls cover placement, size, depth, feather, calibration border and immediate full-room bypass. Network loss, tracking loss and pause/focus loss fail clear. A small status label reports percentage and connection/test/bypass state. Manual Preview is off in the committed scene.

The current compositor layer does not expose its displayed pixels to ordinary Unity post-processing. Official Unity documentation also describes separate camera-image capture, including synchronized stereo GPU capture; this project has Camera Image Support disabled. Correctly replacing native passthrough requires calibrated camera-to-eye reprojection and coverage handling. No raw camera quad or claim that capture is universally impossible was introduced. See [setup and rendering decision](../potentiometer-control.md) for official references and the future true-blur path.

## Checks actually performed

| Check | Result |
| --- | --- |
| Python bridge tests | 6 passed, Python 3.12.10 |
| Actual localhost UDP with simulated fragmented serial input and subsequent silence | Passed as part of bridge tests |
| Both C# scripts compiled against installed Unity 6000.6.9f1 Core, TextRendering and XR assemblies plus .NET Standard 2.1 references | Passed, no compiler diagnostics |
| Launch-scene local IDs, script GUID links, manual mode off, camera transparency, build-list presence, Internet setting | Passed static checks |
| `git diff --check` | Passed |
| Full Unity batch import/compile | Blocked before compilation: licensing-client connection failed; Editor returned code 199 |
| Standalone C# protocol execution attempt | OS Application Control blocked the generated validation assembly; no runtime result claimed |
| Shader compilation / Unity Game tab / Android APK build | Not verified; full Editor validation unavailable |
| Physical COM3 / Nano readings | Not tested; pyserial is not installed in this execution environment |
| Network values in Quest, manual visuals, physical knob visuals, bypass and disconnect in headset | Not tested; headset/ADB path unavailable to this session |

The batch launch log is local at `Logs/knob-validation.log` (ignored by Git). Temporary compile artifacts are under ignored `Temp/KnobValidation`. These code/static checks do not establish headset rendering, native passthrough baseline, stereo alignment or device performance. Follow the staged acceptance checks in the setup guide before calling the hardware milestone verified.
