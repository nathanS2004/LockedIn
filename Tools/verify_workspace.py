"""Compile the fallback runtime/editor and run pure geometry checks with installed Unity assemblies.

Does not claim to validate the optional Meta/YOLO backend, Unity shader compilation,
Android packaging, camera access, or headset accuracy. Run setup and Build and Run for those.
"""
from pathlib import Path
import json
import os
import shutil
import subprocess
import tempfile

root = Path(__file__).resolve().parents[1]
unity = Path(os.environ.get("UNITY_EDITOR_ROOT", r"C:\Program Files\Unity\Hub\Editor\6000.6.9f1\Editor"))
dotnet = shutil.which("dotnet")
if not dotnet:
    raise SystemExit("Install the .NET SDK or add dotnet to PATH.")
sdk = subprocess.check_output([dotnet, "--list-sdks"], text=True).splitlines()[-1]
version, parent = sdk.split(" [", 1)
compiler = Path(parent.rstrip("]")) / version / "Roslyn" / "bincore" / "csc.dll"
(root / "Temp").mkdir(exist_ok=True)
out = Path(tempfile.mkdtemp(prefix="lockedin-workspace-checks-", dir=root / "Temp"))
managed = unity / "Data" / "Managed"
runtime_refs = list((managed / "UnityEngine").glob("UnityEngine*.dll"))
runtime_refs += [unity / "Data" / "NetStandard" / "ref" / "2.1.0" / "netstandard.dll"]
names = ["Unity.InputSystem", "UnityEngine.UI", "Unity.XR.ARFoundation", "Unity.XR.ARSubsystems", "Unity.XR.CoreUtils"]
runtime_refs += [root / "Library" / "ScriptAssemblies" / (name + ".dll") for name in names]
runtime_sources = list((root / "Assets" / "FocusWindow").glob("*.cs"))

def compile_to(name, sources, refs, defines, executable=False):
    target = out / (name + ".dll")
    args = ["-nologo", "-nostdlib+", "-langversion:9.0", "-target:" + ("exe" if executable else "library"),
            '-out:"' + str(target) + '"', "-define:" + defines]
    args += ['-r:"' + str(ref) + '"' for ref in refs]
    args += ['"' + str(source) + '"' for source in sources]
    rsp = out / (name + ".rsp")
    rsp.write_text("\n".join(args), encoding="utf-8")
    subprocess.run([dotnet, str(compiler), "@" + str(rsp)], cwd=root, check=True)
    return target

compile_to("LockedIn.Runtime.Check", runtime_sources, runtime_refs, "UNITY_ANDROID")
editor_refs = runtime_refs + list((managed / "UnityEngine").glob("UnityEditor*.dll"))
editor_refs += [unity / "Data" / "PlaybackEngines" / "AndroidPlayer" / "UnityEditor.Android.Extensions.dll",
                root / "Library" / "ScriptAssemblies" / "Unity.XR.OpenXR.dll"]
compile_to("LockedIn.Editor.Check", list((root / "Assets" / "FocusWindow" / "Editor").glob("*.cs")),
           editor_refs, "UNITY_EDITOR;UNITY_ANDROID")
geometry_sources = [root / "Assets" / "FocusWindow" / name for name in
                    ["WorkspaceGeometry.cs", "WorkspaceDetection.cs", "ScreenDepthSelection.cs"]]
geometry_sources.append(root / "Tests" / "WorkspaceGeometryChecks.cs")
test = compile_to("WorkspaceGeometryChecks", geometry_sources, runtime_refs, "UNITY_ANDROID", True)
for ref in runtime_refs:
    if ref.name != "netstandard.dll":
        shutil.copy2(ref, out / ref.name)
(out / "WorkspaceGeometryChecks.runtimeconfig.json").write_text(json.dumps({"runtimeOptions": {
    "tfm": "net9.0", "framework": {"name": "Microsoft.NETCore.App", "version": "9.0.0"}}}), encoding="utf-8")
subprocess.run([dotnet, str(test)], cwd=out, check=True)
print("Fallback runtime/editor compile checks passed. Optional AI backend still requires Unity setup and headset testing.")
print("Verification output:", out)
