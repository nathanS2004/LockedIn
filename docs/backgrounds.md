# Panoramic backgrounds

Choose Background now includes Sundowner Overlook (replacing Passthrough),
Shanghai Bund (replacing Forest Green), Secluded Beach (replacing Dim Room),
Deep Space (replacing Sunny Meadow), Historic Library and Sunrise Beach.
Refresh Unity, wait for texture imports, Build And Run, create/select a workspace,
then open Choose Background. Before a workspace is selected the existing 45%
opacity cap preserves room visibility; after selection the environment can replace
the surroundings fully and the two workspace regions remain passthrough.
With physical knob control enabled, opacity follows the knob and missing signal
leaves the room clear. See [knob setup](potentiometer-control.md).

The focus-mask shader samples an equirectangular texture using the world-space
view direction. Rotation reveals the photo naturally. Photos are monoscopic and
have no translation parallax: they are not walkable 3D environments. Only the
selected texture is loaded; switching releases the previous one. Original JPEGs
are retained; photo imports use 4K / ASTC 6x6, while NASA Deep Space uses an 8K /
ASTC 4x4 import for star detail. No runtime network is needed for these images.

Validation: runtime, optional AI backend and editor C# compilation passed with
installed Unity assemblies/analyzers. JPEG decoding and 2:1 dimensions verified
for the original three files. The two added downloads have verified 2:1 JPEG dimensions;
their runtime/editor compilation passed, but their headset appearance is not yet verified.
Shader compilation, orientation/seams, clear-window alignment,
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

Sundowner Overlook: Dario Barresi, Poly Haven, CC0.
https://polyhaven.com/a/sundowner_overlook

Shanghai Bund: Greg Zaal, Poly Haven, CC0.
https://polyhaven.com/a/shanghai_bund

The current set has five photographic panoramas and NASA's full-sphere star
visualization. Active assets and credits are listed in
Resources/LockedIn/Backgrounds/Credits.txt. All use 2:1 spherical mapping.
Nature JPEG tone mapping is supplied by Poly Haven. Unity import caps width at
4096 pixels, preserves 2:1 projection, uses sRGB and Android ASTC 6x6 compression.

## Restoring assets from GitHub
The large JPEGs are present in the working Unity project but are not stored in Git. Run `python Tools/download_backgrounds.py` from Windows to download all six active panoramas, then refresh Unity. Credits are included in Resources and this document. Right A bypass and knob minimum retain access to the clear room even though the Passthrough menu tile was replaced.
