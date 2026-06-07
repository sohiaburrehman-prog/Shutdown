#!/usr/bin/env python3
"""Generate Shutdown Timer icons with a solid dark background (no white fringe)."""

from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]

# Match App.xaml theme
BG_RGB = (15, 15, 26)       # #0F0F1A
CYAN = (0, 212, 255)        # #00D4FF
CYAN_DIM = (0, 96, 128)

ICO_SIZES = [16, 24, 32, 48, 64, 128, 256]


def _draw_glyph(draw: ImageDraw.ImageDraw, size: int) -> None:
    cx = cy = size // 2
    radius = int(size * 0.27)
    stroke = max(2, round(size * 0.075))
    thin = max(1, round(size * 0.04))

    ring_box = [cx - radius - thin, cy - radius - thin, cx + radius + thin, cy + radius + thin]

    # Dim timer track
    draw.arc(ring_box, start=200, end=340, fill=CYAN_DIM, width=thin)
    # Active timer segment
    draw.arc(ring_box, start=285, end=355, fill=CYAN, width=stroke)

    # Power ring (open at top)
    power_box = [cx - radius, cy - radius, cx + radius, cy + radius]
    draw.arc(power_box, start=42, end=318, fill=CYAN, width=stroke)

    # Power stem
    stem_top = cy - int(radius * 0.62)
    stem_bottom = cy - int(radius * 0.08)
    draw.line([cx, stem_top, cx, stem_bottom], fill=CYAN, width=stroke)


def render_icon(size: int) -> Image.Image:
    """Draw a crisp power/timer icon on an opaque dark square."""
    img = Image.new("RGB", (size, size), BG_RGB)
    _draw_glyph(ImageDraw.Draw(img), size)
    return img


def render_tray_icon(size: int) -> Image.Image:
    """Tray icon with transparent background so it stays visible on the taskbar."""
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    _draw_glyph(ImageDraw.Draw(img), size)
    return img


def to_rgba(img: Image.Image) -> Image.Image:
    return img.convert("RGBA")


def save_png(path: Path, size: int, *, tray: bool = False) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    image = render_tray_icon(size) if tray else render_icon(size)
    image.save(path, format="PNG", optimize=True)


def save_ico(path: Path, sizes: list[int], *, tray: bool = False) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    largest = max(sizes)
    master = render_tray_icon(largest) if tray else render_icon(largest)
    master.save(
        path,
        format="ICO",
        sizes=[(s, s) for s in sorted(sizes, reverse=True)],
    )


def compose_wide(width: int, height: int) -> Image.Image:
    canvas = Image.new("RGB", (width, height), BG_RGB)
    icon_size = int(min(width, height) * 0.72)
    icon = render_icon(icon_size)
    x = (width - icon_size) // 2
    y = (height - icon_size) // 2
    canvas.paste(icon, (x, y))
    return canvas


def compose_splash(width: int, height: int) -> Image.Image:
    canvas = Image.new("RGB", (width, height), BG_RGB)
    icon_size = int(min(width, height) * 0.5)
    icon = render_icon(icon_size)
    x = (width - icon_size) // 2
    y = (height - icon_size) // 2
    canvas.paste(icon, (x, y))
    return canvas


def write_scaled_square(assets: Path, prefix: str, base: int, scales: list[int]) -> None:
    for scale in scales:
        size = round(base * scale / 100)
        save_png(assets / f"{prefix}.scale-{scale}.png", size)


def main() -> None:
    resources = ROOT / "Resources"
    tray_dir = resources / "TrayIcons"
    assets = ROOT / "Assets"

    save_ico(resources / "app.ico", ICO_SIZES)
    save_png(resources / "logo48.png", 48)
    save_ico(tray_dir / "tray.ico", [16, 24, 32, 48], tray=True)
    save_png(tray_dir / "tray.png", 32, tray=True)

    write_scaled_square(assets, "Square44x44Logo", 44, [100, 125, 150, 200, 400])
    write_scaled_square(assets, "Square150x150Logo", 150, [100, 125, 150, 200, 400])
    write_scaled_square(assets, "StoreLogo", 50, [100, 125, 150, 200, 400])

    for scale in [100, 125, 150, 200]:
        w = round(310 * scale / 100)
        h = round(150 * scale / 100)
        compose_wide(w, h).save(assets / f"Wide310x150Logo.scale-{scale}.png", optimize=True)

    for scale in [100, 125, 150, 200]:
        w = round(620 * scale / 100)
        h = round(300 * scale / 100)
        compose_splash(w, h).save(assets / f"SplashScreen.scale-{scale}.png", optimize=True)

    for size in [16, 24, 32, 48, 256]:
        save_png(assets / f"Square44x44Logo.targetsize-{size}.png", size)

    save_png(assets / "LockScreenLogo.scale-200.png", 48)

    render_icon(1080).save(ROOT / "StoreLogo_1080x1080.png", optimize=True)
    compose_splash(720, 1080).save(ROOT / "StorePoster_720x1080.png", optimize=True)
    save_png(ROOT / "StoreTile_150x150.png", 150)
    save_png(ROOT / "StoreTile_300x300.png", 300)
    save_png(ROOT / "StoreTile_71x71.png", 71)

    render_icon(512).save(resources / "icon-source.png", optimize=True)
    print("Generated programmatic icons (opaque dark background).")


if __name__ == "__main__":
    main()
