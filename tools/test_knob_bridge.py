"""Run with: python -m unittest discover -s tools -p 'test_*.py' -v"""
import contextlib
import io
import socket
import sys
import threading
import time
import types
import unittest
from unittest.mock import patch

import knob_bridge as bridge


class BridgeTests(unittest.TestCase):
    def test_calibration_clamping_and_reversal(self):
        self.assertEqual(bridge.normalize(100, 100, 900), 0)
        self.assertEqual(bridge.normalize(900, 100, 900), 1)
        self.assertEqual(bridge.normalize(500, 100, 900), 0.5)
        self.assertEqual(bridge.normalize(0, 100, 900), 0)
        self.assertEqual(bridge.normalize(1023, 100, 900), 1)
        self.assertEqual(bridge.normalize(900, 900, 100), 0)

    def test_serial_noise(self):
        for line in (b'', b'-1', b'1024', b'1.0', b'booting', b'\xff'):
            self.assertIsNone(bridge.parse_reading(line))
        self.assertEqual(bridge.parse_reading(b'512\r\n'), 512)

    def test_invalid_settings(self):
        for flags in (['--udp-port', '0'], ['--minimum', '1023'], ['--rate', 'nan'],
                      ['--smoothing-ms', '-1'], ['--quest-ip', 'invalid']):
            with contextlib.redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
                bridge.settings(['--quest-ip', '127.0.0.1'] + flags)

    def test_missing_dependency_error(self):
        with patch.dict(sys.modules, {'serial': None}), contextlib.redirect_stderr(io.StringIO()) as log:
            self.assertEqual(bridge.main(['--quest-ip', '127.0.0.1']), 1)
            self.assertIn('pip install', log.getvalue())

    def test_occupied_port_error(self):
        class SerialError(Exception):
            pass
        def occupied(*args, **kwargs):
            raise SerialError('Access denied')
        fake = types.SimpleNamespace(Serial=occupied, SerialException=SerialError)
        with patch.dict(sys.modules, {'serial': fake}), contextlib.redirect_stderr(io.StringIO()) as log:
            self.assertEqual(bridge.main(['--quest-ip', '127.0.0.1']), 1)
            self.assertIn('Close Arduino Serial Monitor', log.getvalue())

    def test_real_udp_with_fragmented_serial_and_stale_input(self):
        # Actual localhost UDP socket; simulated USB only. Silence must stop UDP heartbeats.
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as sink:
            sink.bind(('127.0.0.1', 0))
            sink.settimeout(0.1)
            received = []
            stop = threading.Event()
            def collect():
                while not stop.is_set():
                    try:
                        data, _ = sink.recvfrom(128)
                        received.append((time.monotonic(), data))
                    except socket.timeout:
                        pass
            thread = threading.Thread(target=collect)
            thread.start()
            class Serial:
                in_waiting = 1
                def __enter__(self):
                    self.start = time.monotonic()
                    self.chunks = iter([b'noise\n10', b'23\n'])
                    return self
                def __exit__(self, *args):
                    pass
                def read(self, count):
                    if time.monotonic() - self.start > 0.65:
                        raise KeyboardInterrupt()
                    time.sleep(0.01)
                    return next(self.chunks, b'')
            fake = types.SimpleNamespace(Serial=lambda *a, **k: Serial(), SerialException=type('SerialError', (Exception,), {}))
            started = time.monotonic()
            try:
                with patch.dict(sys.modules, {'serial': fake}), contextlib.redirect_stdout(io.StringIO()):
                    result = bridge.main(['--quest-ip', '127.0.0.1', '--udp-port', str(sink.getsockname()[1])])
            finally:
                stop.set()
                thread.join(1)
            self.assertEqual(result, 0)
            self.assertGreater(len(received), 2)
            self.assertTrue(all(data == b'LI1 1.000000\n' for _, data in received))
            self.assertLess(received[-1][0] - started, 0.4)


if __name__ == '__main__':
    unittest.main()
