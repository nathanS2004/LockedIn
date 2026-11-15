using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace LockedIn
{
    public sealed class KnobUdpReceiver : MonoBehaviour
    {
        [Range(1, 65535)] public int port = 4210;
        [Min(0.1f)] public float timeoutSeconds = 0.75f;
        [Tooltip("Optional Nano/sender IPv4 address; empty accepts any LAN sender.")]
        public string allowedSender = "";
        readonly object gate = new object();
        UdpClient socket;
        Thread worker;
        volatile bool running;
        float latest;
        long receivedAt;
        string error;

        public bool Connected
        {
            get { lock (gate) return receivedAt != 0 && Age(receivedAt) < timeoutSeconds; }
        }
        public float Value { get { lock (gate) return receivedAt != 0 && Age(receivedAt) < timeoutSeconds ? latest : 0; } }
        public string Status { get { lock (gate) return error ?? (Connected ? "LIVE" : "NO SIGNAL - CLEAR"); } }
        static double Age(long stamp) => (Stopwatch.GetTimestamp() - stamp) / (double)Stopwatch.Frequency;

        public static bool TryParse(string message, out float value)
        {
            value = 0;
            return message != null && message.Length <= 32 && message.StartsWith("LI1 ", StringComparison.Ordinal)
                && float.TryParse(message.Substring(4).Trim(), NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out value)
                && !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0 && value <= 1;
        }

        void OnEnable()
        {
            lock (gate) { receivedAt = 0; error = null; latest = 0; }
            if (port < 1 || port > 65535 || timeoutSeconds < 0.1f || float.IsNaN(timeoutSeconds) || float.IsInfinity(timeoutSeconds))
            { SetError("Invalid UDP port/timeout"); return; }
            IPAddress filter = null;
            if (!string.IsNullOrWhiteSpace(allowedSender) &&
                (!IPAddress.TryParse(allowedSender, out filter) || filter.AddressFamily != AddressFamily.InterNetwork))
            { SetError("Invalid sender IPv4 filter"); return; }
            try
            {
                socket = new UdpClient(AddressFamily.InterNetwork);
                socket.Client.ExclusiveAddressUse = true;
                socket.Client.Bind(new IPEndPoint(IPAddress.Any, port));
                socket.Client.ReceiveTimeout = 250;
                socket.Client.ReceiveBufferSize = 8192;
                running = true;
                var ownedSocket = socket;
                worker = new Thread(() => Receive(ownedSocket, filter)) { IsBackground = true, Name = "LockedIn UDP" };
                worker.Start();
            }
            catch (Exception ex) when (ex is SocketException || ex is ArgumentException)
            { SetError("UDP bind failed: " + ex.Message); socket?.Close(); socket = null; }
        }

        void Receive(UdpClient ownedSocket, IPAddress filter)
        {
            var sender = new IPEndPoint(IPAddress.Any, 0);
            while (running)
            {
                try
                {
                    byte[] bytes = ownedSocket.Receive(ref sender);
                    if ((filter == null || filter.Equals(sender.Address)) && bytes.Length <= 32 &&
                        TryParse(Encoding.ASCII.GetString(bytes), out float value))
                    {
                        lock (gate) { latest = value; receivedAt = Stopwatch.GetTimestamp(); }
                    }
                }
                catch (SocketException ex)
                {
                    if (!running) break;
                    if (ex.SocketErrorCode == SocketError.TimedOut) continue;
                    SetError("UDP receive failed: " + ex.SocketErrorCode); break;
                }
                catch (ObjectDisposedException) { break; }
            }
        }

        void SetError(string message) { lock (gate) { error = message; receivedAt = 0; } }
        void OnDisable()
        {
            running = false;
            socket?.Close();
            worker?.Join(500);
            worker = null;
            socket = null;
            lock (gate) receivedAt = 0;
        }
    }
}
