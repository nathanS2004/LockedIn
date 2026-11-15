# Add direct Nano control, surrounding room blur, and Quest handoff docs

LockedIn now keeps the complete workspace-selection app while a physical Nano
33 IoT knob controls its surroundings. The Nano sends directly over Wi-Fi to the
standalone Quest APK; a laptop may supply USB power but is not a data relay.

## Behavior

- Preserve Home, manual borders, depth screen selection, YOLO screen/keyboard
  selection and photo backgrounds. Accepted workspace regions stay clear.
- Map knob values to photo/color opacity. Replace Slate Blue with Room Blur:
  Gaussian-filter actual left/right camera images, depth-reproject valid regions,
  and blend into soft camera-derived fill around the rest of the workspace.
- Add a background UDP receiver, calibrated/smoothed firmware, reconnect/status
  handling and immediate clear on zero, bypass or loss of signal. Keep existing
  controller bindings. Retain the older Python bridge as an optional diagnostic.
- Add a dedicated potentiometer page in a five-slide walkthrough, plus
  setup instructions and a session record distinguishing observations from tests.

## Scope and reproducibility

The feature branch and current main started before the Home/detection milestones.
This PR therefore restores the complete app sources from `b3eb760` as well as
adding knob/blur integration. It also includes the SDK compatibility and Android
manifest/Linear-color-space repairs needed to build that app on Unity 6000.6.9f1.

Run `python Tools/restore_full_app.py` on a fresh checkout before opening Unity.
It restores pinned/verified model and Meta SDK assets, applies reproducible Unity
6.6 SDK repairs, and downloads licensed panoramas. Those large downloads, APKs,
build caches and private `arduino_secrets.h` are excluded. The example header and
instructions cover local credentials, firmware upload, 3V3/GND/A0 wiring, Quest IP,
same-network requirements and USB-only power. No credentials are included.

## Validation

- Nano 33 IoT sketch compiled with SAMD 1.8.14 and WiFiNINA 2.1.1; user confirmed
  firmware upload and live knob data was visible in the headset.
- Six Python tests passed, including real localhost UDP with simulated serial.
- Last complete C# suite: 53 checks passed; real Unity/Meta/Inference assemblies
  were used for fallback, AI/runtime and editor compilation.
- Six D3D shader vertex/fragment checks passed, including instanced stereo.
- Android Room Blur APK built successfully in 204 seconds, installed and launched
  on Quest. User confirmed blur, then requested a fix for its square coverage.
- The final surrounding-fill follow-up passes shader checks and C# compilation.
  Its latest executable rerun passed geometry/opacity checks, then Windows
  Application Control blocked the protocol executable; no full rerun pass claimed.

## Remaining device checks / limitations

Room Blur is experimental. RGB camera coverage is smaller than native passthrough;
the peripheral fill **approximates unseen surroundings**, rather than filtering
unavailable compositor pixels. Stereo timestamps use a tolerance, not guaranteed
hardware synchronization. Final soft-fill APK appearance, stereo alignment,
moving-object/edge behavior, frame time, disconnect cases and AI-camera handoff
still need the device checklist. The updated tutorial and docs expose these limits.

See `docs/devlog/2026-11-14-potentiometer-session-handoff.md` for the complete
user-action/build/troubleshooting record, and `docs/room-blur.md` for the checklist.
