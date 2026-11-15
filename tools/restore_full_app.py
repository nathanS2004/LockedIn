"""Restore the complete app's existing binary assets and patched Meta 85 dependencies.

Run from Windows: python Tools/restore_full_app.py
Requires Internet access; package archives and model are verified before use.
"""
from pathlib import Path
import hashlib
import json
import shutil
import subprocess
import sys
import tarfile
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
CACHE = ROOT / "Temp" / "full-app-dependencies"
MODEL_URL = "https://raw.githubusercontent.com/oculus-samples/Unity-PassthroughCameraApiSamples/3baac1a1a53aa00af3f639425914f14238a97f84/Assets/PassthroughCameraApiSamples/MultiObjectDetection/SentisInference/Model/yolov9sentis.sentis"


def windows_path(path):
    # Meta's SDK includes paths longer than the legacy Windows 260-character limit.
    return "\\\\?\\" + str(path.resolve()) if sys.platform == "win32" else str(path)


def restore_package(name):
    target = ROOT / "Packages" / name
    if target.exists():
        if json.loads((target / "package.json").read_text())["version"] != "85.0.0":
            raise RuntimeError("Different embedded SDK version exists: " + str(target))
        print("Preserving embedded package:", name, flush=True)
        return
    with urllib.request.urlopen("https://packages.unity.com/" + name, timeout=30) as response:
        package = json.load(response)["versions"]["85.0.0"]
    archive = CACHE / (name + ".tgz")
    if not archive.exists():
        print("Downloading", name, flush=True)
        temporary = archive.with_suffix(".download")
        urllib.request.urlretrieve(package["dist"]["tarball"], temporary)
        temporary.replace(archive)
    if hashlib.sha1(archive.read_bytes()).hexdigest() != package["dist"]["shasum"]:
        raise RuntimeError("Package checksum mismatch: " + str(archive))
    extracted = CACHE / name
    extracted.mkdir(exist_ok=True)
    with tarfile.open(archive) as contents:
        contents.extractall(windows_path(extracted), filter="data")
    shutil.copytree(windows_path(extracted / "package"), windows_path(target))


def main():
    CACHE.mkdir(parents=True, exist_ok=True)
    for name in ("com.meta.xr.sdk.core", "com.meta.xr.mrutilitykit"):
        restore_package(name)
    subprocess.run([sys.executable, str(ROOT / "Tools/repair_unity66_packages.py")], cwd=ROOT, check=True)
    model = ROOT / "Assets/Resources/LockedIn/yolov9sentis.sentis"
    if model.exists():
        data = model.read_bytes()
    else:
        with urllib.request.urlopen(MODEL_URL, timeout=60) as response:
            data = response.read()
    digest = hashlib.sha1(b"blob " + str(len(data)).encode() + b"\0" + data).hexdigest()
    if len(data) != 2365288 or digest != "926a4f3b79930c69d57d333dc767c54bcc7c625a":
        raise RuntimeError("YOLO model failed pinned size/hash validation")
    model.parent.mkdir(parents=True, exist_ok=True)
    if not model.exists():
        model.write_bytes(data)
    subprocess.run([sys.executable, str(ROOT / "Tools/download_backgrounds.py")], cwd=ROOT, check=True)
    print("Full-app assets restored. Return to Unity and let Package Manager/import finish.")


if __name__ == "__main__":
    main()
