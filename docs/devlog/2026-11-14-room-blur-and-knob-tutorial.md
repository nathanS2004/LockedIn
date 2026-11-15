# Room Blur and the physical-knob tutorial

The user requested replacing Slate Blue with real-room passthrough blur, with
knob-controlled blur strength, and an additional tutorial slide. Clarified that
the target is the real room, not the existing photo backgrounds.

Replaced Slate with Room Blur in the original background menu and added an
experimental RGB/depth rendering path using existing Meta 85 dependencies.
The implementation filters actual left/right camera pixels with separable Gaussian
passes, then projects each display eye's depth-derived surface point into the
corresponding capture-time RGB image. It does not attempt to blur a Unity GrabPass,
replace the rig, or stretch a mono/flat camera view over the room. Existing native
passthrough remains the clear fallback, and existing screen/keyboard masks remain.

Enabled Android Meta Quest Occlusion. Camera + scene permissions and the AI
symbol/dependencies were already present. The blur path releases cameras for AI
scanning, menus, bypass, zero, disconnect and pause. It rejects missing/stale depth
or RGB data, excessive stereo timestamp skew and missing tracking. Invalid pixels
and coverage edges remain native clear passthrough. Other backgrounds still use
the knob for opacity.

Room Blur is **experimental and partial-coverage**. Captured camera field of view
does not cover the headset's entire native passthrough. A 35 ms timestamp tolerance
is not guaranteed hardware stereo synchronization. Near/far occlusion edges,
moving objects, image orientation, perceptual alignment and performance require
the device checklist; source math alone cannot establish them. See
[implementation and testing notes](../room-blur.md) for explicit limits.

The walkthrough now has five pages. Page 4 explains the Nano's physical knob,
blur versus opacity, zero/disconnect and USB power/direct Wi-Fi. Versioned
completion uses `LockedIn.Walkthrough.v2` so existing users see the updated guide
once; How to use LockedIn can replay it. Existing controller bindings and
detection algorithms remain unchanged.

## Validation

- Fallback runtime, Android AI/runtime and editor source compilation passed
  against real installed Unity, Meta camera/depth and Inference Engine assemblies.
- 53 executable checks passed: 13 existing geometry/filtering, 12 opacity,
  15 protocol, 13 RGB projection/crop/per-eye-baseline checks.
- Six Windows D3D shader vertex/fragment compilations passed with Unity's real
  headers, including mono and instanced stereo. This caught a reserved HLSL
  `point` identifier, which was corrected before the build. The tool explicitly
  distinguishes these syntax checks from Vulkan/mobile validation.
- User brought Unity forward, refreshed, and reported no red import errors.
- User triggered Build And Run. Unity log confirms **Succeeded**, 204 seconds,
  at November 14, 2026, 23:31:55 Phoenix time. Android source/shader packaging
  finished, APK installed to the connected Quest, and launch was requested.
- Updated Gradle APK size is 81,946,275 bytes. Unity's chosen output was
  `lockedln-knob.apk` at the project root (ignored).
- ADB confirms an app process after installation. The inspected PID-filtered
  log did not contain diagnostic output; no visual correctness is inferred from it.
- On-device visual blur, alignment, frame time, tutorial navigation and physical
  knob behavior remain awaiting the user's headset check.

No additional SDK or Arduino firmware change was needed. The Nano still sends
the same normalized UDP value to Quest 192.168.1.171:4210 and may remain plugged
into the laptop only for power. No merge or push was performed.
