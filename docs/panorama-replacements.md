# Panorama replacements

Requested menu replacements:

| Previous tile | New environment | Resource |
| --- | --- | --- |
| Passthrough | Sundowner Overlook | `LockedIn/Backgrounds/sundowner_overlook` |
| Forest Green | Shanghai Bund | `LockedIn/Backgrounds/shanghai_bund` |
| Dim Room | Secluded Beach | `LockedIn/Backgrounds/secluded_beach` |
| Sunny Meadow | Deep Space (NASA star map) | `LockedIn/Backgrounds/space_stars` |

The hidden Passthrough enum remains for internal clear-view semantics, but its
menu tile is replaced. Right A bypass and knob minimum still restore native
passthrough. The new environments use the existing panorama renderer, clear-workspace
mask and knob-controlled opacity. Room Blur and other backgrounds are unchanged.
Menu text uses best-fit sizing so the longer names fit the existing cards.

Downloaded official tone-mapped panoramas:

- [Sundowner Overlook](https://polyhaven.com/a/sundowner_overlook), Dario Barresi,
  CC0: JPEG 8192x4096, 33,985,384 bytes.
- [Shanghai Bund](https://polyhaven.com/a/shanghai_bund), Greg Zaal,
  CC0: JPEG 8192x4096, 4,758,520 bytes.
- [Secluded Beach](https://polyhaven.com/a/secluded_beach), Poly Haven,
  CC0: JPEG 8192x4096, 7,853,973 bytes.
- [Deep Space / NASA Deep Star Maps](https://svs.gsfc.nasa.gov/3895/):
  JPEG 16384x8192, 21,594,829 bytes; credit NASA/Goddard Scientific Visualization Studio.

Photographic imports cap Android textures at 4096 pixels with sRGB / ASTC 6x6.
Deep Space uses NASA's 16K JPEG star visualization, imported at 8192 pixels with
ASTC 4x4 and maximum compression quality to retain small star details. It is a
star-catalog visualization, not a Poly Haven photograph. All retain 2:1 spherical
mapping and are monoscopic still environments, not walkable scenes. Credits and
source links are in Resources. The large files stay local/ignored;
`python tools/download_backgrounds.py` restores all six active panoramas.

Validation: actual download headers confirm JPEG and 2:1 dimensions. Runtime,
Android AI/runtime and editor code compiled with the real installed dependencies.
The latest Secluded Beach/Deep Space check ran all 53 geometry/opacity/protocol/
projection checks successfully. New image orientation, seams, menu readability
and appearance still need Unity import, APK rebuild/install and headset checking.

Return to Unity > Assets > Refresh and wait for texture import, then Build And
Run the updated LockedInPassthrough launch scene. Choose either new environment
after selecting a workspace, close Home and turn the knob. Check that screen and
keyboard stay clear and that A or knob minimum restores the room.
