# Nano sends directly to Quest

The user requested removal of the laptop relay and supplied Quest IPv4
`192.168.1.171`. Added `Arduino/LockedInKnobWiFi/LockedInKnobWiFi.ino`, using
WiFiNINA and the existing `LI1` UDP protocol at destination port 4210. The current
Unity launch scene already has that port, an empty sender filter and the receiver
wired to the full app's environment opacity. Only receiver wording changed from
"laptop" to "sender"; detection/rendering behavior is unchanged.

The new firmware explicitly uses 10-bit A0 readings, configurable/reversible
calibration, 50 ms exponential smoothing, exact endpoints and roughly 30 Hz UDP.
It reconnects after Wi-Fi loss and resamples after reconnect instead of replaying
an old value. USB Serial logging is optional: there is no Serial wait, allowing
boot from a USB power bank. Repeated local send failures restart the UDP/Wi-Fi
setup. The LED/log indicate local connection/sending, not headset acknowledgement;
the Quest LIVE label remains the receipt check.

Existing 3V3/GND/A0 wiring is preserved. Uploading this separately named sketch
will replace the board's previous serial-only firmware, as required for the new
direct path. Python files remain available as an optional legacy diagnostic path,
but should not run alongside this sender.

Created a local Git-ignored `arduino_secrets.h` containing placeholders and a
committable example header. Actual SSID/password must be entered locally before
upload; no credentials were requested in chat, read from the system or printed.

Validation: installed WiFiNINA 2.1.1 and Arduino_SpiNINA 0.0.2 using the existing
Arduino CLI. Compiled the final sketch with Arduino SAMD Boards 1.8.14 for
`arduino:samd:nano_33_iot`: 42,204 bytes flash (16%), 4,312 bytes global RAM (13%).
`git check-ignore` confirms the private header is ignored, and `git diff --check`
passes. Official Arduino hardware/WiFiNINA documentation was consulted; links and
local upload/standalone instructions are in the sketch README.

No firmware upload, Wi-Fi association, packet reception on Quest or power-bank
test was performed. Those require local Wi-Fi credentials and the physical
devices. Existing Quest timeout clears opacity after 0.75 s without valid input.
The headset needs no USB after APK installation; Nano needs a USB power source,
and both devices need LAN connectivity. The computer is only for initial upload,
APK installation and optional diagnostics.
