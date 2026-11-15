// Arduino Nano 33 IoT -> Wi-Fi UDP -> standalone Quest. No laptop bridge.
#include <SPI.h>
#include <WiFiNINA.h>
#include <WiFiUdp.h>
#include <math.h>
#include "arduino_secrets.h"

// Quest IPv4 address. Update if the headset's DHCP address changes.
const IPAddress QUEST_IP(192, 168, 1, 171);
constexpr uint16_t QUEST_PORT = 4210;
constexpr uint16_t LOCAL_PORT = 4211;
constexpr int KNOB_MIN = 0;
constexpr int KNOB_MAX = 1023;  // Reverse endpoints to reverse rotation.
constexpr float SMOOTHING_MS = 50.0f;
constexpr uint32_t SAMPLE_MS = 5;
constexpr uint32_t SEND_MS = 33;
constexpr uint32_t RECONNECT_MS = 5000;
static_assert(KNOB_MIN >= 0 && KNOB_MIN <= 1023 && KNOB_MAX >= 0 && KNOB_MAX <= 1023,
              "Calibration endpoints must be in 0..1023");
static_assert(KNOB_MIN != KNOB_MAX, "Calibration endpoints must differ");
static_assert(QUEST_PORT > 0 && LOCAL_PORT > 0, "UDP ports must be nonzero");
static_assert(SMOOTHING_MS >= 0, "Smoothing must be nonnegative");

WiFiUDP udp;
bool configured = false, connected = false, filterReady = false, attempted = false;
uint32_t lastSample = 0, lastSend = 0, lastRetry = 0, lastStatus = 0, lastLog = 0;
float filtered = 0;
int raw = 0;
uint32_t sentPackets = 0, sendFailures = 0;

void sampleKnob(uint32_t now)
{
    uint32_t elapsed = now - lastSample;
    if (elapsed < SAMPLE_MS && filterReady) return;
    lastSample = now;
    raw = analogRead(A0);
    float target = constrain(float(raw - KNOB_MIN) / float(KNOB_MAX - KNOB_MIN), 0.0f, 1.0f);
    float alpha = SMOOTHING_MS == 0 ? 1.0f : 1.0f - expf(-float(elapsed) / SMOOTHING_MS);
    if (!filterReady || elapsed > 250) filtered = target;
    else filtered += alpha * (target - filtered);
    if (target == 0 || target == 1) filtered = target; // Exact fully clear/opaque endpoints.
    filterReady = true;
}

void connectWiFi()
{
    attempted = true;
    if (Serial) Serial.println("Joining Wi-Fi (Quest and Nano must share a reachable LAN)...");
    // WiFi.begin may wait for the radio. The Quest watchdog remains clear meanwhile.
    int status = strlen(SECRET_PASS) == 0 ? WiFi.begin(SECRET_SSID) : WiFi.begin(SECRET_SSID, SECRET_PASS);
    lastRetry = millis();
    connected = status == WL_CONNECTED && udp.begin(LOCAL_PORT) == 1;
    filterReady = false; // Never transmit an old sample after a slow reconnect.
    if (connected && Serial)
    {
        Serial.print("Nano IP: "); Serial.println(WiFi.localIP());
        Serial.print("Sending LI1 UDP to "); Serial.print(QUEST_IP);
        Serial.print(':'); Serial.println(QUEST_PORT);
    }
    else if (!connected && Serial)
        Serial.println("Wi-Fi/UDP not ready. Check 2.4 GHz SSID/password. Retrying in 5 seconds.");
}

void setup()
{
    pinMode(LED_BUILTIN, OUTPUT);
    analogReadResolution(10); // Preserve the existing 0..1023 calibration.
    Serial.begin(115200);
    // Do not wait for Serial: the sketch must start from a USB power bank.
    configured = strlen(SECRET_SSID) > 0 && strcmp(SECRET_SSID, "YOUR_WIFI_NAME") != 0 &&
        strcmp(SECRET_PASS, "YOUR_WIFI_PASSWORD") != 0;
    if (WiFi.status() == WL_NO_MODULE) configured = false;
}

void loop()
{
    uint32_t now = millis();
    sampleKnob(now);
    if (!configured)
    {
        digitalWrite(LED_BUILTIN, LOW);
        if (Serial && now - lastLog >= 2000)
        {
            Serial.println("Not configured: edit arduino_secrets.h; confirm Nano 33 IoT and working NINA radio.");
            lastLog = now;
        }
        return;
    }

    if (now - lastStatus >= 250)
    {
        lastStatus = now;
        if (WiFi.status() != WL_CONNECTED && connected)
        {
            connected = false;
            udp.stop();
            filterReady = false;
            if (Serial) Serial.println("Wi-Fi lost; Quest clears on timeout. Reconnecting...");
        }
    }
    if (!connected)
    {
        digitalWrite(LED_BUILTIN, (now / 250) % 2 ? HIGH : LOW);
        if (!attempted || now - lastRetry >= RECONNECT_MS) connectWiFi();
        return;
    }

    digitalWrite(LED_BUILTIN, HIGH); // Joined/sending, not a Quest acknowledgement.
    if (now - lastSend >= SEND_MS)
    {
        lastSend = now;
        // Integer formatting works without embedded printf floating-point support.
        unsigned long millionths = (unsigned long)(filtered * 1000000.0f + 0.5f);
        char packet[24];
        int length = snprintf(packet, sizeof(packet), "LI1 %lu.%06lu\n", millionths / 1000000UL, millionths % 1000000UL);
        bool sent = false;
        if (length > 0 && length < (int)sizeof(packet) && udp.beginPacket(QUEST_IP, QUEST_PORT) == 1)
        {
            size_t written = udp.write((const uint8_t*)packet, (size_t)length);
            int completed = udp.endPacket();
            sent = written == (size_t)length && completed == 1;
        }
        if (sent) { sentPackets++; sendFailures = 0; }
        else sendFailures++;
        if (sendFailures >= 10)
        {
            udp.stop();
            connected = false;
            attempted = false;
            sendFailures = 0;
        }
    }
    if (Serial && now - lastLog >= 1000)
    {
        Serial.print("Knob "); Serial.print(raw);
        Serial.print(" -> "); Serial.print(filtered * 100.0f, 0);
        Serial.print("% | packets queued: "); Serial.print(sentPackets);
        Serial.print(" | consecutive send failures: "); Serial.println(sendFailures);
        lastLog = now;
    }
}
