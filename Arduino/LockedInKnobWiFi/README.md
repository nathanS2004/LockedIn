# Direct Nano 33 IoT to Quest

Runtime connection: **potentiometer → Nano 33 IoT → Wi-Fi → Quest**.
The destination is already `192.168.1.171`, UDP port `4210`.
No Python bridge or laptop is needed at runtime.

1. Open `LockedInKnobWiFi.ino` in Arduino IDE.
2. Open the **arduino_secrets.h** tab and fill in `SECRET_SSID` and `SECRET_PASS`.
   A placeholder file has been created locally and is ignored by Git. On a fresh
   checkout copy `arduino_secrets.example.h` to `arduino_secrets.h` first.
   Enter credentials locally; do not send the password in chat or commit it.
3. Select **Arduino Nano 33 IoT** under Arduino SAMD Boards and its USB port
   (previously COM3). WiFiNINA 2.1.1 and Arduino_SpiNINA 0.0.2 were installed
   locally and used for compilation. Library Manager can install WiFiNINA on
   another computer. This is the IoT board, not Nano 33 BLE or a classic Nano.
4. Click **Upload**. This replaces the board's serial-only program with the
   direct Wi-Fi sender. The 3V3/GND/A0 wiring stays exactly the same.
5. Optionally open Serial Monitor at **115200**. It shows the Nano IP, target and
   knob percentage. A steady built-in LED means Wi-Fi/UDP is ready; blinking
   means reconnecting. Neither LED nor a queued UDP packet acknowledges Quest
   receipt: the headset's **LIVE** label confirms reception.
6. Run the restored LockedIn APK. Choose/confirm a workspace, select a background
   and turn the knob. Home/AI/manual controls remain unchanged. The receiver
   should use port 4210 and an empty `allowedSender`, or filter to the **Nano's**
   IP (not the old laptop IP). Stop any Python bridge so two senders cannot compete.
7. After programming, disconnect the Nano from the laptop and power it through
   its USB connector from a USB power bank or USB wall supply. The sketch does
   not wait for a serial connection. After the APK is installed, unplug Quest
   from the computer too and launch the app from Unknown Sources. Both devices
   still need Wi-Fi and their own power.

Use a **2.4 GHz** Wi-Fi network accessible to both devices. The Quest may use
another band on the same LAN if the router allows communication between bands.
Use a normal personal/open network; this sketch doesn't implement enterprise
username/login or captive-portal flows. Guest/client isolation blocks direct UDP.
When moving to another location, changing the virtual background alone needs no
network changes. If using a different Wi-Fi network, connect Quest to it, update
`SECRET_SSID`/`SECRET_PASS` in the private header and set `QUEST_IP` to the
headset's IPv4 address on that new network, then upload the sketch again.
Unity does not need another build just for new network credentials/IP: the app
listens on UDP port 4210. Check **LIVE** inside Home to confirm reception.

If Quest changes IP, update `QUEST_IP` in the sketch and upload again; a router
DHCP reservation for the Quest avoids that. Wi-Fi credentials can also be changed
only by editing the private header and uploading.

`KNOB_MIN`/`KNOB_MAX` configure calibration and may be reversed. ADC resolution
is explicitly 10-bit. Readings are sampled at 200 Hz with a 50 ms exponential
filter; endpoints snap to exactly 0/1. UDP sends roughly 30 Hz using the existing
`LI1 0.500000\n` ASCII protocol. Readings are refreshed after reconnection rather
than replaying a pre-disconnect sample. Wi-Fi retry can take several seconds;
Quest's existing 0.75 s watchdog leaves the environment clear meanwhile.

Test minimum, midpoint, maximum, right-A bypass, then unplug Nano power. The
environment should clear after about 0.75 seconds. Restore power: the Nano joins
Wi-Fi and the LIVE label returns. Finally close the laptop and repeat to confirm
that it is not relaying data. A successfully queued packet is not proof of delivery.

Compilation performed with Arduino SAMD Boards 1.8.14 and WiFiNINA 2.1.1 for
`arduino:samd:nano_33_iot`. Upload, Wi-Fi association and real headset control
have not been performed by the agent; they require local credentials and hardware.

Official references: [Arduino Wi-Fi setup](https://docs.arduino.cc/tutorials/nano-33-iot/wifi-connection/),
[Nano 33 IoT hardware](https://docs.arduino.cc/hardware/nano-33-iot/),
[WiFiNINA UDP API](https://github.com/arduino-libraries/WiFiNINA/blob/master/docs/api.md).
