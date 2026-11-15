using System;
using UnityEngine;

namespace LockedIn
{
    /// <summary>Runs presentation motion independently of the detector and controller input loop.</summary>
    public sealed class FocusUiMotion : MonoBehaviour
    {
        public Action<float> Tick;
        private void Update() => Tick?.Invoke(Mathf.Min(Time.unscaledDeltaTime, 0.05f));
        private void OnDestroy() => Tick = null;
    }
}
