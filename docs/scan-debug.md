# Debug a workspace scan on Quest

Build And Run once to install the new build. From then on, you can relaunch it from the headset Library. Open Home with the right grip, select **Scan Debug**, then **Start debug**. Frame the screen and keyboard, point at the center of the screen and press the right trigger. Hold your head still until review.

The panel shows the frozen camera image and up to 12 numbered model boxes. Mint means the box passed the 2D filter. Amber means it was filtered, for example for low confidence, size or overlap. Neither color indicates a successful 3D fit. The separate cyan borders in the room show the final fitted regions. Press the trigger to retry without saving the preview, A to accept it, or grip to leave. A failed scan can still show boxes if the camera captured a frame. Camera images stay in memory until you retry or leave. Only numeric results and diagnostic text go into the device log.

On the same desk, do ten scans without changing lighting or aim. Note the scan number and whether the screen or keyboard is missing or clipped. Collect the log while the headset is connected over USB and authorized:

```powershell
$adb = 'C:\Program Files\Unity\Hub\Editor\6000.6.9f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe'
& $adb logcat -d -s Unity:I | Select-String -SimpleMatch '[LockedIn Scan #' | Set-Content lockedin-scan-log.txt
```

Compare one good and one bad scan by number. If YOLO emits no keyboard box, start with framing or the model. If the keyboard score is under 0.35, investigate the cutoff. If the box passes filtering but depth fitting fails, look at valid depth samples and the fit status. If fitting succeeds but the border is short, compare the frozen 2D box and the final 3D border. The pinned model may suppress some proposals internally before the app receives them.

Runtime, AI-enabled runtime, and editor compilation passed with Unity analyzers. The 13 existing geometry and filtering checks passed. Quest rendering and camera alignment await a device test.
