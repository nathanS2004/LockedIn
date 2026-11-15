"""Windows D3D syntax/instancing checks using actual Unity includes.

This does NOT validate Unity shader import, Vulkan/mobile compilation, GPU output,
stereo alignment, or frame time on Quest. Run the Android build/device checklist too.
"""
import ctypes as c
import os
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
INCLUDES = Path(os.environ.get("UNITY_EDITOR_ROOT", r"C:\Program Files\Unity\Hub\Editor\6000.6.9f1\Editor")) / "Data/Resources/CGIncludes"


class Macro(c.Structure):
    _fields_ = [("Name", c.c_char_p), ("Definition", c.c_char_p)]


def blob_bytes(blob):
    if not blob.value:
        return b""
    table = c.cast(blob, c.POINTER(c.POINTER(c.c_void_p))).contents
    pointer = c.WINFUNCTYPE(c.c_void_p, c.c_void_p)(table[3])(blob)
    length = c.WINFUNCTYPE(c.c_size_t, c.c_void_p)(table[4])(blob)
    data = c.string_at(pointer, length)
    c.WINFUNCTYPE(c.c_ulong, c.c_void_p)(table[2])(blob)
    return data


def main():
    if os.name != "nt":
        raise SystemExit("This syntax check uses Windows D3DCompile.")
    compiler = c.WinDLL("d3dcompiler_47.dll").D3DCompile
    compiler.argtypes = [c.c_void_p, c.c_size_t, c.c_char_p, c.POINTER(Macro), c.c_void_p,
                        c.c_char_p, c.c_char_p, c.c_uint, c.c_uint, c.POINTER(c.c_void_p), c.POINTER(c.c_void_p)]
    compiler.restype = c.c_long
    out = ROOT / "Temp/BlurShaderChecks"
    out.mkdir(parents=True, exist_ok=True)
    count = 0
    original_directory = Path.cwd()
    try:
        os.chdir(INCLUDES)  # Standard include handler resolves Unity's nested includes here.
        for name in ("Assets/FocusWindow/FocusMask.shader", "Assets/Resources/RoomCameraBlur.shader"):
            text = (ROOT / name).read_text().split("CGPROGRAM", 1)[1].split("ENDCG", 1)[0]
            text = re.sub(r"^\s*#pragma.*$", "", text, flags=re.M)
            text = text.replace('#include "RoomBlurReprojection.cginc"',
                '#include "' + (ROOT / "Assets/FocusWindow/RoomBlurReprojection.cginc").as_posix() + '"')
            text = '#include "HLSLSupport.cginc"\n' + text
            path = out / (Path(name).stem + ".hlsl")
            path.write_text(text)
            data = text.encode()
            for stereo in (False, True) if path.stem == "FocusMask" else (False,):
                for entry, profile, stage in (("vert" if path.stem == "FocusMask" else "vert_img", "vs_5_0", "SHADER_STAGE_VERTEX"),
                                              ("frag", "ps_5_0", "SHADER_STAGE_FRAGMENT")):
                    definitions = [("SHADER_API_D3D11", "1"), ("SHADER_TARGET", "35"), ("UNITY_VERSION", "600060"), (stage, "1")]
                    if stereo:
                        definitions += [("STEREO_INSTANCING_ON", "1"), ("INSTANCING_ON", "1")]
                    macros = (Macro * (len(definitions) + 1))(*[Macro(a.encode(), b.encode()) for a, b in definitions], Macro(None, None))
                    bytecode, errors = c.c_void_p(), c.c_void_p()
                    result = compiler(data, len(data), str(path).encode(), macros, 1, entry.encode(), profile.encode(), 1 << 12, 0,
                                      c.byref(bytecode), c.byref(errors))
                    details = blob_bytes(errors).decode(errors="replace")
                    blob_bytes(bytecode)
                    if result < 0:
                        raise RuntimeError(f"{path.stem} {entry} stereo={stereo}:\n" + details)
                    count += 1
    finally:
        os.chdir(original_directory)
    print("PASS:", count, "D3D vertex/fragment syntax checks including mono + instanced stereo.")
    print("Quest Vulkan/mobile, alignment and performance remain unverified.")


if __name__ == "__main__":
    main()
