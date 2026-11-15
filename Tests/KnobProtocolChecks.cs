using System;
using LockedIn;

public static class KnobProtocolChecks
{
    public static int Main()
    {
        string[] valid = { "LI1 0", "LI1 1.000000\n", "LI1 0.512300\r\n" };
        string[] invalid = { null, "", "0.5", "LI2 0.5", "LI1 NaN", "LI1 Infinity",
            "LI1 -0.1", "LI1 1.01", "LI1 0,5", "LI1 1e-1", "LI1 0.5\nLI1 0.6", "LI1 " + new string('0', 40) };
        foreach (string packet in valid)
            if (!KnobUdpReceiver.TryParse(packet, out float value) || value < 0 || value > 1)
                throw new Exception("Rejected valid packet: " + packet);
        foreach (string packet in invalid)
            if (KnobUdpReceiver.TryParse(packet, out float value))
                throw new Exception("Accepted invalid packet: " + packet);
        Console.WriteLine("PASS: 15 knob protocol checks");
        return 0;
    }
}
