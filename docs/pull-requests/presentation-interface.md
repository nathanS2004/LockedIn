# Redesign the native Quest interface for presentation

Replace the compact dark menu with a warm paper/ink/orange study console, photographic environment collection and a live potentiometer instrument. Add controller hover/press feedback, supported haptics, guided calibration, progress/previous controls in the five-slide tutorial, and accessible image/font credits.

Remove the floating in-session percentage/status strip; the knob dial remains inside Home. Document how to update Nano credentials and the Quest destination address when moving networks.

Preserve AI screen/keyboard detection, depth selection, manual calibration, Nano Wi-Fi control, clear-workspace masks and environment rendering. Generate small menu thumbnails in the editor rather than loading every full panorama on Quest. Include font licenses, documentation and Unity-rendered review captures.

Validation: runtime, Android AI and editor compilation; 53 existing checks; Unity import without red errors; editor ray-navigation fixture across detector/environment/help routes. Quest rebuild, stereo readability, frame timing and physical controller feedback require headset validation. Preview screenshots use an explicitly documented editor-only sample signal.

Stacked on `feature/secluded-beach-space` so the PR contains only the interface pass.
