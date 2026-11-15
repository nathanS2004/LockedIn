# Edit detected borders on Quest

1. Run **Detect Workspace (AI)** from Home. Wait for the cyan preview. Press **A** to accept it as-is, **B** to edit, or the trigger to scan again.
2. In Edit Borders, point at the screen or keyboard border with the right controller. Hold the trigger near an edge and move the controller to resize. Hold the trigger inside the border and move to reposition it. The selected outline turns gold. You can also select the region from the panel.
3. If a region is missing, select **Add keyboard** or **Add screen**. Point at a real corner and pull the trigger, then point at the diagonally opposite corner and pull the trigger again. The first point uses headset depth. Keyboard placement uses a horizontal plane. Re-adding a screen uses the original detected screen orientation. Press **B** to leave placement without adding anything.
4. Select **Apply** to use the draft. **Undo** reverses the latest move, resize, add or remove. **Cancel** or the right grip restores the previous workspace. Apply is available once the draft contains at least one border.

The draft supports one screen and one keyboard region. It keeps their 3D planes as you move and resize them. A keyboard-only draft uses the first clear region when applied.

## Headset smoke test

- Scan a laptop and expand only the keyboard front edge. Check that the screen border stays aligned and the keyboard remains clear when you move your head.
- Scan with the keyboard hidden, reveal it and use Add keyboard. Check that the two corners land on the keyboard plane.
- Undo a removal, then cancel and verify the previous accepted workspace returns.
- Repeat with a panorama and room blur. Editing should show passthrough, and applying should restore the selected background and knob control.
