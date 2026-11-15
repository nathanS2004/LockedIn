"""USB Arduino -> UDP. No serial or camera access runs on the Quest."""
import argparse
import ipaddress
import math
import socket
import sys
import time


def normalize(raw, minimum, maximum):
    return max(0.0, min(1.0, (raw - minimum) / (maximum - minimum)))


def parse_reading(line):
    try:
        text = line.decode("ascii").strip()
        if not text or not text.isdecimal():
            return None
        value = int(text)
        return value if 0 <= value <= 1023 else None
    except (UnicodeError, ValueError):
        return None


def settings(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--serial-port", default="COM3")
    parser.add_argument("--baud", type=int, default=115200)
    parser.add_argument("--quest-ip", required=True, help="Quest IPv4 address")
    parser.add_argument("--udp-port", type=int, default=4210)
    parser.add_argument("--minimum", type=int, default=0)
    parser.add_argument("--maximum", type=int, default=1023,
                        help="May be below minimum to reverse knob direction")
    parser.add_argument("--smoothing-ms", type=float, default=50)
    parser.add_argument("--rate", type=float, default=30)
    parser.add_argument("--serial-only", action="store_true", help="Print without sending UDP")
    args = parser.parse_args(argv)
    try:
        address = ipaddress.IPv4Address(args.quest_ip)
        if address.is_multicast or address.is_unspecified or str(address) == "255.255.255.255":
            raise ValueError()
    except ValueError:
        parser.error("--quest-ip must be a unicast IPv4 address")
    if not args.serial_port.strip() or args.baud <= 0:
        parser.error("Serial port must be nonempty and baud must be positive")
    if not 1 <= args.udp_port <= 65535:
        parser.error("--udp-port must be between 1 and 65535")
    if not (0 <= args.minimum <= 1023 and 0 <= args.maximum <= 1023) or args.minimum == args.maximum:
        parser.error("Calibration endpoints must be distinct integers in 0..1023")
    if not math.isfinite(args.smoothing_ms) or not 0 <= args.smoothing_ms <= 1000:
        parser.error("--smoothing-ms must be finite and in 0..1000")
    if not math.isfinite(args.rate) or not 1 <= args.rate <= 120:
        parser.error("--rate must be finite and in 1..120 Hz")
    return args


def main(argv=None):
    args = settings(argv)
    try:
        import serial
    except ImportError:
        print("Missing pyserial. Run: python -m pip install -r tools/requirements.txt", file=sys.stderr)
        return 1
    try:
        with serial.Serial(args.serial_port, args.baud, timeout=0.05) as source, \
                socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as udp:
            print(f"Reading {args.serial_port} at {args.baud}; target {args.quest_ip}:{args.udp_port}")
            print("Close Arduino Serial Monitor. Ctrl+C stops; Quest clears after timeout.")
            filtered = None
            last_valid = last_sent = last_print = time.monotonic()
            pending = bytearray()
            while True:
                # Bound memory and drain queued USB data; use only the latest complete valid line.
                chunk = source.read(min(max(source.in_waiting, 1), 4096))
                pending.extend(chunk)
                newest = None
                while b"\n" in pending:
                    line, _, rest = pending.partition(b"\n")
                    pending = bytearray(rest)
                    raw = parse_reading(line) if len(line) <= 16 else None
                    if raw is not None:
                        newest = raw
                if len(pending) > 64:
                    pending.clear()
                now = time.monotonic()
                if newest is not None:
                    target = normalize(newest, args.minimum, args.maximum)
                    dt = now - last_valid
                    alpha = 1 if args.smoothing_ms == 0 else -math.expm1(-dt / (args.smoothing_ms / 1000))
                    filtered = target if filtered is None or dt > 0.5 else filtered + alpha * (target - filtered)
                    # Endpoints must be exact, especially zero (fully clear).
                    if target in (0.0, 1.0):
                        filtered = target
                    last_valid = now
                if filtered is not None and now - last_valid < 0.25 and now - last_sent >= 1 / args.rate:
                    if not args.serial_only:
                        udp.sendto(f"LI1 {filtered:.6f}\n".encode("ascii"), (args.quest_ip, args.udp_port))
                    last_sent = now
                if now - last_print >= 1:
                    state = f"{filtered:.0%}" if filtered is not None and now - last_valid < 0.25 else "NO VALID SERIAL DATA (not sending)"
                    print(state, flush=True)
                    last_print = now
    except serial.SerialException as exc:
        print(f"Cannot read {args.serial_port}: {exc}. Close Arduino Serial Monitor and other bridges; check cable and COM port in Device Manager.", file=sys.stderr)
        return 1
    except OSError as exc:
        print(f"Serial/network error: {exc}. Check device, Quest IP and Wi-Fi connection.", file=sys.stderr)
        return 1
    except KeyboardInterrupt:
        print("Stopped. Quest will clear on timeout.")
        return 0


if __name__ == "__main__":
    sys.exit(main())
