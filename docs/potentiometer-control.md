# Physical knob: existing app environment opacity

The complete LockedIn app is restored from milestone snapshot `b3eb760`: Home,
first-use walkthrough, backgrounds, depth screen selection, manual borders and
YOLO workspace detection. The knob controls the **existing FocusWindow renderer's
opacity**. It does not add another black overlay, replace detection, change
accepted screen/keyboard volumes or add conflicting controller bindings.

- Minimum: the selected environment becomes fully transparent; the room is visible.
- Maximum: after selecting/confirming a workspace, the environment becomes opaque
  around it. The screen/keyboard passthrough regions stay unchanged.
- Intermediate values: the chosen image or color blends over native passthrough.
- Right A outside menus/scans: existing immediate background bypass toggle.
- No valid knob packets: environment clears after the 0.75 s receiver timeout.
- Menus, scan instructions and cyan review borders still work without knob data.

Before a workspace is accepted, the original 45% opacity cap remains. Choosing
Passthrough always leaves the room visible. Photos remain monoscopic panoramas,
not walkable scenes. This is environment opacity/compositing, **not true blur**.
The existing stereo-aware shader and its world-space clear volumes are retained.

## Room Blur

**Room Blur** replaces Slate Blue. In this experimental mode the knob controls
a true Gaussian filter on real left/right camera images instead of environment
opacity. Valid RGB/depth regions use projected camera blur; uncovered or rejected
pixels stay native-clear. This initial PR does not include surrounding soft fill;
that improvement is intentionally separate. See [implementation and device checklist](room-blur.md).

Select/confirm a workspace, choose Room Blur, close Home with right grip, and turn
the knob. Allow camera + scene access if prompted. Zero, bypass, signal loss,
pause/focus/tracking loss and stale camera/depth data restore native clear view.
The version-2 tutorial adds a dedicated physical-knob slide and replays once for
existing users. Other photo/color backgrounds retain knob-controlled opacity.

## Direct Nano Wi-Fi (current setup)

The Nano now sends straight to **Quest 192.168.1.171:4210**. The laptop and Python
bridge are not needed at runtime. Open
`Arduino/LockedInKnobWiFi/LockedInKnobWiFi.ino` in Arduino IDE, fill in the local
Git-ignored `arduino_secrets.h`, select Nano 33 IoT and upload once over USB.
Power the Nano from a USB power bank or wall USB supply afterward. Launch the
installed Quest APK without its USB cable. Keep the existing 3V3/GND/A0 wiring.
Both devices must be on a mutually reachable LAN; Nano uses 2.4 GHz Wi-Fi.
Stop any old Python bridge so it cannot compete with the Nano.

See [direct Wi-Fi sketch and full instructions](../Arduino/LockedInKnobWiFi/README.md).
WiFiNINA 2.1.1 and the Nano SAMD board toolchain are installed locally. The sketch
was compiled for the real board target; Wi-Fi association/upload still require
local credentials and hardware. Do not share the Wi-Fi password in chat.

## Unity: restore, import, build

Binary assets and patched Meta SDKs have been restored locally. On a fresh checkout:

```powershell
python Tools/restore_full_app.py
```

This restores the three existing licensed background JPEGs, verifies Meta Core
and MRUK 85 archives against registry hashes, applies the complete app's existing
Unity 6.6 compatibility repairs, and validates the pinned YOLO model size/blob hash.
Downloads/embedded SDKs are ignored by Git; the scripts and license notices are
preserved. Required Inference Engine 2.6.1 and transitive packages resolve through
Unity Package Manager from the manifest. No model training or external inference
service is added.

1. Return to Unity 6000.6.9f1, use Assets > Refresh if necessary, and wait for
   package resolution, texture/model import and compilation. Existing open Editors
   may need the scene reopened after imports to see external scene-file changes.
   Discard any unsaved old scene version when prompted, so it cannot overwrite
   the restored scene on disk.
2. Open `Assets/Scenes/LockedInPassthrough.unity`. Main Camera has **FocusWindow**;
   **LockedIn Knob Receiver** has **KnobUdpReceiver**. There is no standalone
   FocusAreaObscuring component or replacement clear-area object.
3. On the receiver, configure UDP `port` (4210), `timeoutSeconds` (0.75) and optional
   `allowedSender` Nano/sender IPv4. Restart the app after socket setting changes.
   Main Camera > FocusWindow references this receiver and enables
   **Knob Controls Opacity**, with a 60 ms smoothing time constant.
4. Android settings include `LOCKEDIN_AI`, Meta XR Feature, Meta Quest Session,
   Camera/Passthrough, Composition Layers, Raycasts and Touch input. Native
   transparent-camera passthrough and the active stereo mode are retained.
   The correct serialized `ForceInternetPermission` setting is enabled; the
   build callback also requests INTERNET and AI camera/scene permissions.
5. If restoring on a different Editor installation leaves AI setup incomplete,
   run **LockedIn > Set Up AI Workspace Detection** after imports. It preserves
   embedded SDK compatibility fixes and configures the AI symbol/Meta XR Feature.
6. Build Profiles > Meta Quest > select Quest under Run Device > Build And Run.
   Keep LockedInPassthrough first in the scene list. Use the standalone installed
   APK, not Link. The app should launch into the original walkthrough/Home UI.

Disabling **Knob Controls Opacity** in the Inspector restores the original
controller-only opacity behavior, including the Dim Room thumbstick adjustment.
It is enabled by default in the launch scene. With it enabled, the knob controls
all selected colors/photos/Dim Room across the full 0?1 range; the thumbstick does
not compete with the hardware.

## Optional legacy Windows bridge

The older serial-only sketch can still use the optional laptop bridge. That
path is not needed for direct Nano Wi-Fi. Its wiring remains: Nano 33 IoT connected by USB to
Windows, outer potentiometer terminals at 3V3/GND, center at A0, integers
0?1023 per line at 115200 baud. Quest never opens a Windows COM port.
Close Arduino Serial Monitor/Serial Plotter before starting the bridge.

From the project folder in PowerShell:

```powershell
python -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r tools/requirements.txt
.\.venv\Scripts\python.exe -m serial.tools.list_ports
.\.venv\Scripts\python.exe tools/knob_bridge.py --serial-port COM3 --quest-ip 192.168.1.50 --udp-port 4210
```

Replace the example IP with Quest's IPv4 address from Meta Quest Developer Hub
Device Manager/device details or headset Settings > Wi-Fi > connected network
details. With ADB available, `adb shell ip -4 addr show wlan0` also shows it.
COM3 is configurable; confirm the actual port in Device Manager after reconnecting.
Ctrl+C stops the bridge. The percentage printed on Windows should follow the knob.

Calibration and smoothing:

```powershell
.\.venv\Scripts\python.exe tools/knob_bridge.py --serial-port COM3 --quest-ip 192.168.1.50 --minimum 12 --maximum 1005 --smoothing-ms 50 --rate 30
```

Endpoints may be reversed to reverse rotation. Minimum/maximum snap to exactly
0/1. Invalid settings, missing pyserial, occupied serial ports and network errors
produce useful messages. Serial boot text and out-of-range readings are ignored.
Without valid serial input, the bridge stops sending after 250 ms; it never
repeats an old value indefinitely. With the receiver's 0.75 s watchdog, the room
clears within about one second of serial silence, on the next rendered frame.
Zero, bypass, pause/focus loss and disconnect clear without a smoothing tail.

Laptop and Quest need mutually reachable IPv4 addresses, usually on the same
non-guest Wi-Fi. Check guest/client/AP isolation, VPNs and different VLANs if the
headset stays NO SIGNAL. Allow Python outbound UDP on the private network;
Editor receiver testing also requires Unity inbound UDP. A successful UDP send
does not acknowledge delivery. Verify IP, port, foreground APK and sender filter.
The protocol is one short ASCII datagram, e.g. `LI1 0.500000` followed by newline;
invalid messages do not keep the watchdog alive. This trusted-LAN prototype has
no authentication or ordering guarantee; do not expose the receiver publicly.

## Existing app controls

Right controller trigger selects menu buttons/scans/records manual points.
Right grip opens/closes Home or cancels scanning. Use Home > Detect Workspace
(AI) to scan the screen/keyboard and grant camera/scene permissions. Right A
accepts review borders; trigger retries; right B switches to manual. Auto Select
Screen keeps the existing depth-only path. Create Border retains the existing
three-corner physical calibration and optional keyboard extension.

After accepting a workspace, open Home > Choose Background, select a photo or
color, and close Home with the grip. Turn the knob. Right A bypasses/restores the
background outside menus/scans; it still confirms a workspace in review. Right B
still starts manual recalibration. Accepted borders stay fixed in room coordinates
for the session; rescan after moving the laptop or resetting tracking origin.

The compact lower-view label reads **Environment N% | LIVE / NO SIGNAL - CLEAR**.
It reports actual applied opacity, including the pre-selection 45% cap and bypass.
The home UI remains visible at zero or without signal.

## Legacy bridge acceptance checks

For the current direct Nano path, use the acceptance steps in the Arduino README.
These checks below apply to the older optional serial-to-laptop bridge.

1. Serial only: `python tools/knob_bridge.py --serial-port COM3 --quest-ip 127.0.0.1 --serial-only`.
   Check minimum/midpoint/maximum. Choose calibrated endpoints.
2. Network: run the bridge to Quest and check LIVE/percentage. Home should be
   present without a connection. Stop the bridge and verify NO SIGNAL.
3. Existing app: finish the walkthrough and use AI, depth selection, or manual
   placement. Confirm screen/keyboard borders. Choose Library, Beach or Meadow.
4. Turn the knob from minimum to maximum. Confirm smooth environment alpha,
   full room at minimum, full environment at maximum after workspace acceptance,
   unchanged clear regions and correct both-eye/head-motion behavior. Change
   backgrounds and repeat; no additional black overlay should appear.
5. At high opacity, press A for immediate bypass, change the knob, then A to
   restore current strength. Stop bridge, unplug Nano and interrupt Wi-Fi
   separately. Each must clear. Reconnect and check LIVE resumes. Pause/resume
   the app, test the menu/scan controls again and measure on-device frame time.

Local checks: `python -m unittest discover -s tools -p 'test_*.py' -v` and
`python Tools/verify_workspace.py`. The latter compiles runtime/editor sources
against installed Unity assemblies, runs geometry/opacity checks and compiles the
AI backend only when Unity has generated the real dependency assemblies.
These do not establish shader/Android/headset success. See the integration log
for results and remaining device verification.
