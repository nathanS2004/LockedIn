# Replace Passthrough and Forest Green with Poly Haven environments

Stacked on #14. This PR adds only the requested panorama replacements and their
documentation; the initial knob integration and surrounding-fill work remain
separate in #13 and #14.

- Replace Passthrough's menu tile with **Sundowner Overlook** and Forest Green
  with **Shanghai Bund**, using the existing panorama renderer and knob opacity.
- Preserve clear-workspace masks, right-A bypass, zero/disconnect clearing and
  all other environment/detection controls. Size long menu names to fit.
- Extend the downloader to restore both official tone-mapped JPEGs, and add CC0
  credits, tutorial wording, setup instructions and a dated development log.

The 8K JPEGs are downloaded locally and ignored with the other binary assets.
On a fresh checkout run `python tools/download_backgrounds.py`, then refresh
Unity and wait for import. Existing Android import caps remain 4K / ASTC 6x6.

Validation: JPEG headers and 8192x4096 dimensions verified for both images;
runtime, Android AI/runtime and editor compilation passed. Forty executable
geometry/opacity/protocol checks passed before Windows Application Control
blocked the generated projection test. No projection or shader logic changed.
`git diff --check` passes.

Remaining: rebuild/install on Quest and check image orientation/seams, menu names,
clear workspace, knob opacity, A bypass and zero/disconnect behavior. New photo
appearance is not yet headset-verified.

Details: `docs/panorama-replacements.md` and
`docs/devlog/2026-11-15-polyhaven-environments.md`.
