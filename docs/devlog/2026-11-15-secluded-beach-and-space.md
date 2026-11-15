# Secluded Beach and Deep Space

The user requested Secluded Beach and another high-quality, space-themed option,
replacing Dim Room and Sunny Meadow respectively. The new menu choices reuse
existing panorama rendering, clear screen/keyboard volumes and knob opacity.
The hidden Dimmed enum remains for legacy/default behavior but is no longer a
menu choice. Right A bypass and knob minimum still show the room.

Downloaded Secluded Beach's official tone-mapped JPEG from Poly Haven. Selected
NASA's Deep Star Maps 16K JPEG as Deep Space: a full-sphere celestial panorama,
not a rectangular space wallpaper. The JPEG fits the existing LDR renderer
without adding HDR exposure conversion or another rendering pipeline. This is
the 2012 ready-to-display map, not the newer 2020 EXR dataset.

Source links and credits:
- [Secluded Beach](https://polyhaven.com/a/secluded_beach), Poly Haven, CC0.
- [Deep Star Maps](https://svs.gsfc.nasa.gov/3895/), NASA/Goddard Scientific
  Visualization Studio; Ernie Wright and Tom Bridgman.
- [NASA media guidelines](https://www.nasa.gov/nasa-brand-center/images-and-media/).

The star map is imported at 8K with ASTC 4x4 / maximum compression quality rather
than the default 4K / ASTC 6x6 photo settings. This preserves more star detail but
costs roughly 43 MiB of GPU texture memory with mipmaps. Only the selected panorama
is loaded, and switching releases the previous asset. Actual appearance and frame
time remain unverified on Quest. All backgrounds are monoscopic environments.

The old, locally downloaded Meadow image and metadata were preserved under
ignored `Temp/retired-panorama-assets` outside Resources so they do not inflate
the next APK. The downloader no longer fetches Meadow and now restores the six
active panoramas, including these two. Large images remain ignored by Git.

Validation: both downloads have JPEG headers and 2:1 dimensions. Runtime fallback,
Android AI/runtime and editor compilation passed, as did all 53 existing
geometry/opacity/protocol/camera-projection checks. No new implementation-mirroring
tests were added. Refresh Unity, wait for image import, and rebuild/install to
check the new images, long labels, seams, screen clarity, knob opacity and bypass.
