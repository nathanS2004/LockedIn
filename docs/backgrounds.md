# Photographic backgrounds

Choose Background now includes Historic Library, Sunrise Beach, and Sunny Meadow.
Refresh Unity, wait for texture imports, Build And Run, create/select a workspace,
then open Choose Background. Before a workspace is selected the existing 45%
opacity cap preserves room visibility; after selection the environment replaces
the background fully and the two workspace regions remain passthrough.

The focus-mask shader samples an equirectangular texture using the world-space
view direction. Rotation reveals the photo naturally. Photos are monoscopic and
have no translation parallax: they are not walkable 3D environments. Only the
selected texture is loaded; switching releases the previous one. Original JPEGs
are retained; Android imports are capped at 4K with ASTC 6x6. No runtime network.

Validation: runtime, optional AI backend and editor C# compilation passed with
installed Unity assemblies/analyzers. JPEG decoding and 2:1 dimensions verified
for all three files. Shader compilation, orientation/seams, clear-window alignment,
and final texture compression/appearance still need Unity and Quest validation.

## Photo credits

LockedIn photographic panorama credits

Historic Library: Biblioteca Publica de Evora - Sala Nova (360 panorama).
Photographer: Waldyrious, 6 April 2024.
Source: https://commons.wikimedia.org/wiki/File:Biblioteca_P%C3%BAblica_de_%C3%89vora_-_Sala_Nova_(360_panorama).jpg
License: Creative Commons Attribution-ShareAlike 4.0 International
https://creativecommons.org/licenses/by-sa/4.0/
Original JPEG is included unchanged. Unity scales/compresses it for the headset.
The scaled/compressed photo remains CC BY-SA 4.0. No endorsement is implied.

Sunrise Beach: Umhlanga Sunrise, Poly Haven.
https://polyhaven.com/a/umhlanga_sunrise
https://dl.polyhaven.org/file/ph-assets/HDRIs/extra/Tonemapped%20JPG/umhlanga_sunrise.jpg

Sunny Meadow: Meadow 2, Poly Haven.
https://polyhaven.com/a/meadow_2
https://dl.polyhaven.org/file/ph-assets/HDRIs/extra/Tonemapped%20JPG/meadow_2.jpg
Both nature photos: CC0 https://creativecommons.org/publicdomain/zero/1.0/
License statement: https://polyhaven.com/license

All three are photographic 360x180 equirectangular still images, not generated art.
Nature JPEG tone mapping is supplied by Poly Haven. Unity import caps width at
4096 pixels, preserves 2:1 projection, uses sRGB and Android ASTC 6x6 compression.

## Restoring assets from GitHub
The original large JPEGs are present in the working Unity project but are not stored in this commit. Run `python Tools/download_backgrounds.py` from Windows to download the three originals, then refresh Unity. Credits are included in Resources and this document.
