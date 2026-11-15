# Fill room-blur boundaries with a soft surrounding environment

Stacked on #13. Review the initial Nano/control/blur implementation there first;
this PR contains only the subsequent surrounding-fill change and its documentation.

The first Room Blur build blurred a square matching valid camera/depth coverage.
The user confirmed that behavior and requested an environment-like effect around
the whole clear workspace. Native passthrough exposes no matching full-field
camera texture, so the user explicitly chose approximate camera-derived soft fill.

## Change

- Keep depth-reprojected, Gaussian-filtered RGB where supported. Blend rejected
  or uncovered pixels into a shared, heavily softened 16x12 color field.
- Use a continuous directional mapping so the peripheral fill has no rear seam.
  Both eyes share this low-detail field rather than displaying a misaligned
  second camera view. Accepted screen/keyboard masks remain fully clear.
- Preserve knob-controlled strength, zero/bypass/disconnect clearing, camera
  freshness/tracking guards and AI camera handoff.
- Update tutorial/background explanations and document the split PR sequence.

The surrounding fill approximates unseen colors; it is not true filtering or
reconstruction of the native compositor's unseen room pixels.

## Validation

Six desktop D3D shader syntax checks pass, including mono and instanced stereo.
Runtime/AI/editor source compilation passed against actual dependencies. The
initial partial-coverage APK's build/install and visible blur were confirmed in
#13; those results do not validate the new full surrounding fill.

Rebuild/install and check full surrounding coverage, clear workspace boundaries,
both eyes, head motion, frame time, knob endpoints, A bypass, disconnect and
re-entering AI. The final soft-fill APK appearance remains unverified.
