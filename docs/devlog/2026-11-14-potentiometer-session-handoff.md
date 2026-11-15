# Potentiometer integration: session record and handoff

November 14, 2026, America/Phoenix. Earlier milestone logs are preserved as
historical records, including the AI milestone's existing November 15 filename.
This record distinguishes user observations, code checks and unfinished tests.

## Intended result and current architecture

Keep the complete LockedIn application: Home, help, backgrounds, manual borders,
depth screen selection and YOLO screen/keyboard selection. The physical knob
controls photo/color opacity or Room Blur strength, without changing accepted
clear-workspace volumes. The Nano 33 IoT reads A0 and sends normalized `LI1` UDP
datagrams directly to the supplied Quest address `192.168.1.171`, port `4210`.

Quest runs a standalone installed APK without a computer tether. The Nano may
stay connected to the laptop **only for USB power**, because a separate power
supply was unavailable. No laptop relay/Python process is needed. The earlier
serial-to-Python bridge remains an optional diagnostic alternative.

Room Blur replaces Slate Blue. Valid pixels use stereo camera images and depth
reprojection; the latest requested revision adds a heavily softened camera-derived
fill around the rest of the workspace. This fill approximates unseen colors,
not true filtering of unavailable native-compositor pixels. The tutorial has
five pages, including the physical knob, direct Wi-Fi and fail-clear behavior.

## User actions and observations

| Action or observation | Evidence / result |
| --- | --- |
| Opened existing LockedIn project in Unity Hub, using 6000.6.9f1 | Screenshots show the project and Editor |
| Opened LockedInPassthrough after initially seeing Untitled/FocusLensDemo | Scene screenshots and build list |
| Checked Console and selected Meta Quest target | Screenshots show informational messages and Meta Quest active |
| Connected Quest using USB and Meta Quest Developer Hub | Run Device screenshot lists Quest 3 |
| Installed/launched initial knob prototype | Reported `Obscuring 0% / NO SIGNAL - CLEAR` |
| Identified missing complete detection app | Led to Git-history inspection and restoration |
| Clarified that original app must remain and knob should control its environment | Final integration uses existing FocusWindow renderer |
| Supplied Quest IP and requested direct Nano Wi-Fi | Firmware now targets that configurable address |
| Explained Nano must use laptop USB power | Sketch starts without waiting for Serial; no relay needed |
| Entered credentials locally and reported Arduino upload | User explicitly confirmed upload; credentials are ignored by Git |
| Saw live percentages in headset | Photo showed `Obscuring 31% | LIVE` in the old prototype |
| Checked Player Color Space | Screenshot showed Linear after project-file correction loaded |
| Refreshed complete app and retried builds | Editor/ADB logs confirm later successful install and launch |
| Requested real-room blur and a new potentiometer tutorial slide | Camera/depth blur and version-2 walkthrough added |
| Refreshed blur sources and reported no red errors | Explicit user confirmation |
| Triggered Room Blur Build And Run | Unity log confirms success in 204 seconds, install and launch |
| Confirmed visible blur but only in a square | Actual headset observation; not a full stereo/performance pass |
| Chose full surrounding blur with soft fill | Explicitly accepted approximation after its limitation was explained |
| Requested PR and complete documentation | Publishing is authorized by this later request; no merge requested |

Instructions to test a feature are not evidence that the user performed that test.
This session does not establish that all AI/depth/manual flows, disconnect cases,
both-eye alignment or performance checks passed.

## Implementation and troubleshooting

1. Added configurable serial calibration/smoothing, a background UDP receiver,
   strict input validation, status display and 0.75-second fail-clear watchdog.
2. Found the complete app in snapshot `b3eb760`; this feature branch started at
   older passthrough-only `3d33618`. Restored later app sources without merging
   branches or deleting the original virtual demo scene.
3. Connected the receiver to FocusWindow; retired the independent black overlay,
   rectangle and conflicting input system from the first iteration.
4. Restored licensed panoramas, pinned/verified YOLO model and Meta 85 SDKs.
   Added reproducible downloads and Unity 6.6 SDK compatibility repairs. Large
   generated binaries, SDKs, APKs, caches and private Wi-Fi header are ignored.
5. Corrected Android Internet permission serialization, enabled required XR
   features and Linear color space while preserving native transparent passthrough.
6. Fixed obsolete Meta editor callbacks and library-manifest name/icon references.
   Final manifest callback runs after Meta so camera/scene permissions persist.
7. Diagnosed read-only generated Android resource folders; repaired only generated
   Gradle output flags, without Library deletion or project-wide ACL changes.
8. Successfully packaged and directly installed the full app after GUI failures
   had left the old prototype installed. ADB reported successful install/cold launch.
9. Added direct WiFiNINA firmware with calibration, smoothing, reconnect, optional
   Serial diagnostics and standalone boot. Kept existing 3V3/GND/A0 wiring.
10. Added experimental stereo RGB/depth blur, camera sharing with AI, missing-data
    fallbacks and five-page tutorial. Added surrounding soft fill after the user
    reported the square coverage boundary.

## Validation status at PR preparation

- Nano compilation passed with Arduino SAMD 1.8.14 / WiFiNINA 2.1.1: 42,204 bytes
  flash, 4,312 bytes global RAM. User separately confirmed upload.
- Six Python bridge tests passed, including actual localhost UDP with simulated
  fragmented serial data and stopping sends after silence.
- Last complete C# suite: 53 checks passed (13 geometry/filtering, 12 opacity,
  15 protocol, 13 camera projection). Fallback, Android AI/runtime and editor
  compilation used actual installed Unity/Meta/Inference assemblies.
- Six D3D shader checks passed, including mono and instanced stereo.
- First Room Blur Android APK built, installed and launched; user confirmed
  camera blur and its square coverage limit.
- The **soft-fill follow-up** passes shader checks and C# compilation. Its latest
  test rerun completed geometry/opacity checks, but Windows Application Control
  blocked the generated protocol executable. That rerun is not a complete 53-test pass.
- Soft-fill APK appearance is not yet verified. Stereo alignment, motion/edge
  behavior, GPU frame time, final disconnect cases and AI camera handoff remain open.

## Reproduce and continue

- [Nano firmware, credentials, upload and USB power](../../Arduino/LockedInKnobWiFi/README.md)
- [Unity controls/network setup and troubleshooting](../potentiometer-control.md)
- [Room Blur implementation, limitations and device checklist](../room-blur.md)
- [Walkthrough and controller instructions](../instructions.md)
- Fresh checkout: run `python Tools/restore_full_app.py` before opening Unity,
  then let Package Manager resolve/import the dependencies.
- Refresh and Build And Run the latest soft-fill source. Check full surrounding
  fill, clear screen/keyboard, A bypass, zero, signal loss, tracking/pause, returning
  to AI selection and both-eye alignment.

No merge has been requested or performed. The PR should describe the current
soft-fill implementation and retain these unverified-device limits.

## Separate PR sequence

PR #13 contains the initial Nano integration, partial-coverage room blur, tutorial
and handoff documentation. The surrounding soft-fill change is a separate stacked
PR based on that branch. Review the initial implementation first, then the fill
follow-up. Neither PR has been merged.
