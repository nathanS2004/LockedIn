# Replace Dim Room and Meadow with Secluded Beach and Deep Space

Stacked on #15 so this PR contains only the next two requested environment
replacements, without duplicating the earlier panorama changes.

- Replace **Dim Room** with **Secluded Beach** from Poly Haven and **Sunny Meadow**
  with **Deep Space**, using NASA's full-sphere Deep Star Maps panorama.
- Use the official 8K beach JPEG and 16K NASA JPEG source. Import the star map at
  8K with ASTC 4x4 / maximum compression quality to retain small stars; other
  photographic environments keep 4K / ASTC 6x6 settings.
- Preserve existing knob opacity, clear workspace masks, right-A bypass and
  zero/disconnect behavior. Update menu help, downloader, credits and documentation.
- Retire the old Meadow download from Resources and stop fetching it. Large
  images remain ignored and reproducible through `tools/download_backgrounds.py`.

The space environment is a star-catalog visualization, not a Poly Haven CC0
photograph. NASA credits and media-usage references are documented separately.
Panoramas are monoscopic still environments, not walkable scenes. The 8K star
texture costs approximately 43 MiB with mipmaps; only one panorama is loaded.

Validation: both downloads have valid JPEG headers and 2:1 dimensions
(8192x4096 beach, 16384x8192 stars). Runtime fallback, Android AI/runtime and editor
compilation passed, and all 53 existing checks passed. `git diff --check` passes.

Remaining: refresh/import in Unity, rebuild/install, and check image orientation,
seams, star detail, performance, clear workspace, knob opacity and bypass on Quest.
The new environments have not yet been headset-verified.

See `docs/devlog/2026-11-15-secluded-beach-and-space.md` and
`docs/panorama-replacements.md` for sources, credits and setup.
