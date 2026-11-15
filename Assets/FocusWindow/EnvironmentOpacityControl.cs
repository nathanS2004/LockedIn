using System;

namespace LockedIn
{
    // Independent of rendering and detection so timeout/bypass behavior can be checked directly.
    public sealed class EnvironmentOpacityControl
    {
        public float Value { get; private set; }

        public float Update(float requested, bool connected, bool visible, float deltaSeconds, float smoothingSeconds)
        {
            if (!connected || !visible || float.IsNaN(requested) || float.IsInfinity(requested) || requested <= 0)
                return Value = 0;

            float target = Math.Min(requested, 1);
            float alpha = smoothingSeconds <= 0 ? 1 : (float)(1 - Math.Exp(-Math.Max(0, deltaSeconds) / smoothingSeconds));
            Value += (target - Value) * alpha;
            if (target == 1 && Value >= 0.999f) Value = 1;
            return Value;
        }

        public void Clear() { Value = 0; }
    }
}
