using System;
using LockedIn;

public static class EnvironmentOpacityChecks
{
    private static int checks;
    private static void Check(bool result, string message)
    {
        if (!result) throw new Exception(message);
        checks++;
    }

    public static int Main()
    {
        var control = new EnvironmentOpacityControl();
        Check(control.Update(1, true, true, 1, 0) == 1, "Maximum must support full opacity");
        Check(control.Update(0, true, true, 0.01f, 1) == 0, "Minimum must clear immediately");
        control.Update(1, true, true, 1, 0);
        Check(control.Update(1, false, true, 0.01f, 1) == 0, "Disconnect must clear immediately");
        control.Update(1, true, true, 1, 0);
        Check(control.Update(1, true, false, 0.01f, 1) == 0, "Bypass/pause must clear immediately");
        float first = control.Update(0.8f, true, true, 0.02f, 0.06f);
        Check(first > 0 && first < 0.8f, "Reconnect must ease into selected opacity");
        float next = control.Update(0.8f, true, true, 0.02f, 0.06f);
        Check(next > first && next < 0.8f, "Smoothing must approach target monotonically");
        Check(control.Update(float.NaN, true, true, 1, 0) == 0, "NaN must clear");
        Check(control.Update(float.PositiveInfinity, true, true, 1, 0) == 0, "Infinity must clear");
        Check(control.Update(2, true, true, 1, 0) == 1, "Values must be bounded");
        control.Clear();
        Check(control.Value == 0, "Disable must reset smoothing state");
        // Exponential smoothing should be independent of rendering frequency.
        var slow = new EnvironmentOpacityControl();
        var fast = new EnvironmentOpacityControl();
        for (int i = 0; i < 36; i++) slow.Update(0.7f, true, true, 1f / 36, 0.06f);
        for (int i = 0; i < 90; i++) fast.Update(0.7f, true, true, 1f / 90, 0.06f);
        Check(Math.Abs(slow.Value - fast.Value) < 0.00001f, "Smoothing must not depend on frame rate");
        for (int i = 0; i < 100; i++) control.Update(1, true, true, 1f / 72, 0.06f);
        Check(control.Value == 1, "A settled maximum must become exactly opaque");
        Console.WriteLine("PASS: " + checks + " environment opacity checks");
        return 0;
    }
}
