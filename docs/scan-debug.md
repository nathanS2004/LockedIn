# Debug a workspace scan on Quest

Build And Run once to install the new build. From then on, you can relaunch it from the headset Library. Open Home with the right grip, select **Scan Debug**, then **Start debug**. Frame the screen and keyboard, point at the center of the screen and press the right trigger. Hold your head still until review.

The panel shows the frozen camera image and up to 12 numbered model boxes. Mint means the box passed the 2D filter. Amber means it was filtered, for example for low confidence, size or overlap. Neither color indicates a successful 3D fit. The separate cyan borders in the room show the final fitted regions. Press the trigger to retry without saving the preview, A to accept it, or grip to leave. A failed scan can still show boxes if the camera captured a frame. Camera images stay in memory until you retry or leave. Only numeric results and diagnostic text go into the scan file. Each attempt appends its full record, including all relevant model proposals, to the app's persistent storage. The file survives retries and app restarts. Logcat shows a short result line for each completed scan.

On the same desk, do ten scans without changing lighting or aim. Note the scan number and whether the screen or keyboard is missing or clipped. Collect the log while the headset is connected over USB and authorized:

```powershell
$adb = 'C:\Program Files\Unity\Hub\Editor\6000.6.9f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe'
& $adb pull '/sdcard/Android/data/com.DefaultCompany.FocusLens_Unity_Demo/files/lockedin-scan-log.txt' './lockedin-full-scan-log.txt'
```

Compare one good and one bad scan by number and session ID. Scan numbers restart when the app restarts. The summary lists the keyboard source as a YOLO keyboard with depth fit, an estimated laptop base, or none. A low keyboard score can still lead to an estimated base when the lower part of the laptop box supports a depth fit. If YOLO emits no keyboard box, start with framing or the model. If the keyboard score is under 0.35, investigate the cutoff. If the box passes filtering but depth fitting fails, look at valid depth samples and the fit status. If fitting succeeds but the border is short, compare the frozen 2D box and the final 3D border. The pinned model may suppress some proposals internally before the app receives them.

Runtime, AI-enabled runtime, and editor compilation passed with Unity analyzers. The 13 existing geometry and filtering checks passed. Quest rendering and camera alignment await a device test.

The file is created on the first Scan Debug trigger press in the updated build. The old Logcat export remains separate. Uninstalling the app or clearing its data removes the saved scans, so export them first. For a fresh experiment, use the session ID or export the current file and remove it from the app folder while the app is closed.

Persistence and USB export still need verification on the rebuilt APK.

## Integration with the room backgrounds

Scan Debug uses the same detection flow as standard scanning. Starting a debug scan stops room blur while the scan runs, and pausing the app still suspends the selected environment. Scan diagnostics are available alongside the physical knob and the expanded panorama picker.

## Screen selection from normal reach

A single screen or laptop proposal with a confidence of at least 0.65 goes straight to depth verification. Hold the controller comfortably and keep the workspace in the camera view. When there are several screen candidates (or one weaker candidate), aim at the display you want. The app intersects the controller ray with each depth-fitted display plane, favors a hit over a near miss, and chooses the nearest hit when displays overlap along the ray. An aim up to 6 cm outside the fitted border is eligible. The cyan preview still requires A to accept.

Depth serves as the second check. The app tries an edge scan, then falls back to a plane fit from depth samples. This keeps one YOLO inference per trigger. The debug log names the selection method and records depth failures and controller misses in meters. The existing laptop-base estimate still runs when a separate keyboard is unavailable.

The previous device log showed 16 single strong laptop detections rejected by the angle cutoff. All 16 now qualify for depth verification. That replay checks eligibility only. It cannot tell whether the depth fit would pass on those frames.

On Quest, repeat ten scans from your seat with a single laptop, then try scanning toward each of two visible screens. Check that the intended screen is outlined and press A to accept. Compare the result counts with the former 3 of 21 successful scans. Confirm the log distinguishes a single strong detection from a controller-ray selection.
