from PIL import Image
import base64
import os

out = r"C:\Backend project\frontend\subito-web\public\assets"

def write_svg(png_name: str, svg_name: str) -> None:
    png_path = os.path.join(out, png_name)
    im = Image.open(png_path)
    with open(png_path, "rb") as f:
        b64 = base64.b64encode(f.read()).decode("ascii")
    svg = (
        f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {im.width} {im.height}" '
        f'role="img" aria-label="Subito">\n'
        f"  <title>Subito</title>\n"
        f'  <image href="data:image/png;base64,{b64}" width="{im.width}" height="{im.height}"/>\n'
        f"</svg>\n"
    )
    path = os.path.join(out, svg_name)
    with open(path, "w", encoding="utf-8") as f:
        f.write(svg)
    print(svg_name, os.path.getsize(path))

write_svg("logo.png", "logo.svg")
write_svg("logo-mark.png", "logo-mark.svg")
