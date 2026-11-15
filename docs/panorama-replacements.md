# Sundowner Overlook and Shanghai Bund

Requested menu replacements:

| Previous tile | New environment | Resource |
| --- | --- | --- |
| Passthrough | Sundowner Overlook | `LockedIn/Backgrounds/sundowner_overlook` |
| Forest Green | Shanghai Bund | `LockedIn/Backgrounds/shanghai_bund` |

The hidden Passthrough enum remains for internal clear-view semantics, but its
menu tile is replaced. Right A bypass and knob minimum still restore native
passthrough. Both new photos use the existing panorama renderer, clear-workspace
mask and knob-controlled opacity. Room Blur and other backgrounds are unchanged.
Menu text uses best-fit sizing so the longer names fit the existing cards.

Downloaded official tone-mapped panoramas:

- [Sundowner Overlook](https://polyhaven.com/a/sundowner_overlook), Dario Barresi,
  CC0: JPEG 8192x4096, 33,985,384 bytes.
- [Shanghai Bund](https://polyhaven.com/a/shanghai_bund), Greg Zaal,
  CC0: JPEG 8192x4096, 4,758,520 bytes.

Existing texture import rules cap Android textures at 4096 pixels, use sRGB and
ASTC 6x6, and retain 2:1 equirectangular mapping. Images are monoscopic still
panoramas, not walkable scenes. Credits are included in Resources and background
documentation. The large files stay local/ignored; `python tools/download_backgrounds.py`
now restores all five photos on a fresh checkout.

Validation: actual download headers confirm JPEG and 2:1 dimensions. Runtime,
Android AI/runtime and editor code compiled with the real installed dependencies.
40 geometry/opacity/protocol checks executed successfully; Windows Application
Control blocked the generated projection-test executable on this run. The
projection sources were unchanged. New image orientation, seams, menu readability
and appearance still need Unity import, APK rebuild/install and headset checking.

Return to Unity > Assets > Refresh and wait for texture import, then Build And
Run the updated LockedInPassthrough launch scene. Choose either new environment
after selecting a workspace, close Home and turn the knob. Check that screen and
keyboard stay clear and that A or knob minimum restores the room.
