# LockedIn instructions and first-use walkthrough

Implemented November 14, 2026 (Phoenix).

First launch shows four pages: controller controls, workspace selection, backgrounds,
and adjustment/recovery. Completing Get started records a versioned local preference.
Back to Home or right grip skips without marking the walkthrough completed.
Home > How to use LockedIn replays it anytime.

Each mode has a preflight explanation with a Start button and Back to Home:
- AI: look at screen and keyboard, allow camera/scene permissions, aim at screen,
  right trigger scans, hold head still, A accepts preview, trigger retries,
  B starts manual replacement, grip returns to Home.
- Auto Select Screen: depth-based, screen only; success applies immediately.
- Create Border: physically move the controller close to top-left, top-right,
  bottom-left. Cyan dot is 6 cm along the pointer ray, not a distant raycast.
  Trigger records points. Then record front keyboard edge or A for screen only.
- Backgrounds: select a workspace first; otherwise opacity is capped at 45%.
  Passthrough shows the room. Photo panoramas are not walkable environments.

Outside menus/selection, A toggles background, B starts a new manual border,
right thumbstick changes Dim Room strength, right grip opens/closes Home.
The guide explicitly says borders stay in the room and moving the laptop requires
another scan. It does not claim continuous object tracking or automatic correction.

The calibration card was enlarged for readable multi-line prompts. Manual prompts
now describe physical controller placement. Existing scan status and failure/retry
messages remain. No detection algorithm or input binding was changed.

## Validation
Runtime, AI-enabled runtime, and editor C# compilation passed with installed Unity
assemblies/analyzers. Readability, controller selection, page transitions, completion
persistence, and all Start/Back actions need a headset smoke test after Build And Run.

## Demo check
1. First launch: four pages; Next advances; Get started opens Home.
2. Relaunch: Home appears; How to use LockedIn reopens the guide.
3. Each mode: instructions, Back, then Start lead to the correct flow.
4. Manual: inspect dot/corner prompts; finish screen-only and with keyboard.
5. AI: test retry, accept, B manual, grip Home, and permission denial.
6. Backgrounds: browse/select; grip closes; A toggles when focusing.
