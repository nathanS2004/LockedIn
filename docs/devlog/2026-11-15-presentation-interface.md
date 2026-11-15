# Native interface presentation pass ? 15 November 2026

## Request

Redesign the whole in-headset interface for a distinctive, polished presentation while preserving the complete detection application and potentiometer control.

## Implementation

The old dark mint-button menu is replaced by a warm, typographic study console. Three workspace methods remain distinct. A photographic environment entry opens a seven-location collection with hover previews and persistent selection markers. A ticked dial and compact focus strip show actual knob intensity and signal/bypass state. Calibration uses the same paper/ink/orange visual language.

The five-slide guide now has progress marks, previous/next controls and a dedicated physical-knob illustration. Credits remain accessible. Barlow font assets and licenses are included. Low-resolution derivatives of the six credited panoramas are generated in the editor; the menu never loads all full-size panoramas.

Controller hit-testing and callbacks remain native. Hover transitions, press reticle feedback, short page fades and optional right-controller haptics support interaction. No firmware, detector thresholds, clear-region geometry or scene configuration was changed for this pass.

## Verification

- Runtime, Android AI backend and editor source compile against the installed Unity assemblies with analyzers.
- Existing 53 geometry, opacity, UDP protocol and room-camera projection checks pass.
- Unity imports without new red errors (also confirmed by the user).
- The editor fixture passes actual ray navigation through all three detector routes, seven environment selections, credits/back, all five guide pages, guidance/HUD visibility, reopening Home and outside-ray rejection.
- Actual Unity-rendered Home, collection and potentiometer-guide previews are saved in `docs/previews`. Their sample 64% LIVE state is an editor-only fixture, not a claim about a connected Nano or a headset screenshot.

The first capture exposed a heading clipped by its text rectangle and photos not yet imported. Heading bounds and preview asset readiness were corrected before the final visual review. The visual pass also caught and fixed a missing CanvasRenderer requirement on the custom dial; final Unity captures show both dial faces rendering correctly. The existing Windows Burst code-integrity warning is unrelated to these UI changes.

An updated Quest build, stereo readability/performance, physical haptics and presentation rehearsal remain to be verified on the headset. See [interface design and rebuild steps](../interface-design.md).
