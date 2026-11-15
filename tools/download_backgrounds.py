"""Restore the licensed panorama assets (Windows, curl required)."""
from pathlib import Path
import subprocess, hashlib, urllib.parse
root = Path(__file__).resolve().parents[1]
destination = root / "Assets/Resources/LockedIn/Backgrounds"
destination.mkdir(parents=True, exist_ok=True)
library = "Biblioteca Pública de Évora - Sala Nova (360 panorama).jpg".replace(" ", "_")
hash = hashlib.md5(library.encode()).hexdigest()
urls = {
 "library": "https://upload.wikimedia.org/wikipedia/commons/" + hash[0] + "/" + hash[:2] + "/" + urllib.parse.quote(library),
 "beach": "https://dl.polyhaven.org/file/ph-assets/HDRIs/extra/Tonemapped%20JPG/umhlanga_sunrise.jpg",
 "sundowner_overlook": "https://dl.polyhaven.org/file/ph-assets/HDRIs/extra/Tonemapped%20JPG/sundowner_overlook.jpg",
 "shanghai_bund": "https://dl.polyhaven.org/file/ph-assets/HDRIs/extra/Tonemapped%20JPG/shanghai_bund.jpg",
 "secluded_beach": "https://dl.polyhaven.org/file/ph-assets/HDRIs/extra/Tonemapped%20JPG/secluded_beach.jpg",
 "space_stars": "https://svs.gsfc.nasa.gov/vis/a000000/a003800/a003895/starmap_16k.jpg"
}
for name, url in urls.items():
 target = destination / (name + ".jpg")
 if target.exists():
  print("Already present:", target.name)
  continue
 temporary = target.with_suffix(".download")
 subprocess.run(["curl.exe", "--ssl-revoke-best-effort", "--fail", "-L", "--max-time", "180", "-o", str(temporary), url], check=True)
 if temporary.read_bytes()[:2] != b"\xff\xd8":
  raise ValueError("Download is not a JPEG: " + name)
 temporary.replace(target)
print("See Assets/Resources/LockedIn/Backgrounds/Credits.txt for licenses.")
