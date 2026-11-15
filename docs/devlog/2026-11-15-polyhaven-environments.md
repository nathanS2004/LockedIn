# Poly Haven environment replacements

November 15, 2026, America/Phoenix.

The user supplied two Poly Haven links and requested replacing the Passthrough
and Forest Green menu choices. Added Sundowner Overlook in the first slot and
Shanghai Bund in the former green slot. Other choices, workspace detection,
Room Blur, knob-controlled opacity, right-A bypass and zero/disconnect clearing
retain their existing behavior. The internal Passthrough value remains available
for clear-view semantics; it is no longer shown as a background tile.

Downloaded the official 8K tone-mapped JPEGs (both 8192x4096), verified their JPEG
headers and 2:1 dimensions, and extended the reproducible downloader to restore
all five photos. The new files stay local/ignored with the other large assets.
Added CC0 credits for Dario Barresi's Sundowner Overlook and Greg Zaal's Shanghai
Bund. Existing imports cap Android textures at 4K with ASTC 6x6 compression.
Adjusted menu text sizing for long names and updated the walkthrough/setup docs.

Runtime, Android AI/runtime and editor source compilation passed against actual
installed dependencies. Forty geometry/opacity/protocol checks ran successfully;
Windows Application Control blocked the generated projection executable in this
run. Projection logic and shader code were unchanged. `git diff --check` passes.

New image orientation, seams, menu readability and opacity appearance still need
Unity import, APK rebuild/install and a headset check. No new headset success is
claimed merely from downloading or compiling the changes.

The change is on `feature/polyhaven-environments`, separate from the initial
knob PR and surrounding-fill PR. Its PR is stacked on the surrounding-fill branch
so reviewers see only these replacements. Neither existing PR is merged here.

See [complete asset details and test steps](../panorama-replacements.md).
