"""Clear Windows read-only flags on generated Android output; never delete sources."""
import ctypes
import os
from pathlib import Path


def main():
    if os.name != "nt":
        raise SystemExit("This repair is only for Windows read-only build outputs.")
    gradle = Path(__file__).resolve().parents[1] / "Library/Bee/Android/Prj/IL2CPP/Gradle"
    changed = 0
    for relative in ("launcher/build", "unityLibrary/build", "unityLibrary/xrmanifest.androidlib/build", "build"):
        candidate = gradle / relative
        if not candidate.exists():
            continue
        for path in [candidate, *candidate.rglob("*")]:
            if not path.resolve().is_relative_to(gradle.resolve()):
                raise RuntimeError("Cache entry escaped generated Gradle directory: " + str(path))
            extended = "\\\\?\\" + str(path.resolve())
            flags = ctypes.windll.kernel32.GetFileAttributesW(extended)
            if flags == -1:
                raise ctypes.WinError()
            if flags & 1:  # FILE_ATTRIBUTE_READONLY; preserve all other flags.
                if not ctypes.windll.kernel32.SetFileAttributesW(extended, flags & ~1):
                    raise ctypes.WinError()
                changed += 1
    print("Cleared read-only flags on", changed, "generated Android build entries.")


if __name__ == "__main__":
    main()
