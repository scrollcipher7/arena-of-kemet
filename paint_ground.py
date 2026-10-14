import numpy as np
from PIL import Image, ImageDraw, ImageFilter

W = 2048
S = W / 120.0                     # pixels per world unit; world spans -60..60
rng = np.random.default_rng(42)

def px(x): return (x + 60) * S
def py(z): return (60 - z) * S    # image top = +Z (north rim)

def P(x, z): return (px(x), py(z))

# ---------- base earth ----------
img = Image.new("RGBA", (W, W), (107, 84, 58, 255))
d = ImageDraw.Draw(img, "RGBA")
# large soft mottling
for _ in range(500):
    x, z = rng.uniform(-62, 62, 2)
    r = rng.uniform(4, 16)
    _shades = [(122, 98, 66), (96, 74, 50), (116, 94, 62), (88, 68, 46)]
    shade = _shades[int(rng.integers(len(_shades)))]
    d.ellipse([px(x - r), py(z + r), px(x + r), py(z - r)], fill=shade + (60,))
img = img.filter(ImageFilter.GaussianBlur(18))
d = ImageDraw.Draw(img, "RGBA")

# ---------- jungle shade (between the roads, the wild ground) ----------
for _ in range(200):
    x, z = rng.uniform(-58, 58, 2)
    r = rng.uniform(2.5, 7)
    d.ellipse([px(x - r), py(z + r), px(x + r), py(z - r)],
              fill=(62, 68, 36, 42))
img = img.filter(ImageFilter.GaussianBlur(6))
d = ImageDraw.Draw(img, "RGBA")

LANES = [
    [(-50, -50), (-50, 38), (-38, 50), (38, 50), (50, 38), (50, 50)],   # Top
    [(-50, -50), (0, 0), (50, 50)],                                     # Mid
    [(-50, -50), (-38, -50), (38, -50), (50, -38), (50, 50)],           # Bot
]

def stroke(pts, width_units, color):
    d.line([P(x, z) for x, z in pts], fill=color, width=int(width_units * S),
           joint="curve")

def disc(x, z, r, color):
    d.ellipse([px(x - r), py(z + r), px(x + r), py(z - r)], fill=color)

# ---------- the river: banks first, then the water ----------
stroke([(-24, 24), (24, -24)], 10.5, (138, 115, 80))
disc(-24, 24, 15.5, (138, 115, 80)); disc(24, -24, 15.5, (138, 115, 80))
stroke([(-24, 24), (24, -24)], 7.2, (20, 64, 96))
disc(-24, 24, 13.2, (20, 64, 96)); disc(24, -24, 13.2, (20, 64, 96))
stroke([(-24, 24), (24, -24)], 3.6, (32, 92, 128))
disc(-24, 24, 8.5, (32, 92, 128)); disc(24, -24, 8.5, (32, 92, 128))

# ---------- the roads, drawn into the earth ----------
for lane in LANES:
    stroke(lane, 8.4, (66, 54, 36))          # the dark edge
    stroke(lane, 7.0, (166, 148, 118))       # the stone
    stroke(lane, 5.6, (176, 158, 126))       # the worn heart of the road

# tower pads at the march fractions both courts build on
def path_point(pts, frac):
    segs = []
    total = 0.0
    for i in range(len(pts) - 1):
        L = np.hypot(pts[i + 1][0] - pts[i][0], pts[i + 1][1] - pts[i][1])
        segs.append(L); total += L
    want = frac * total
    for i, L in enumerate(segs):
        if want <= L:
            t = want / L if L > 0 else 0
            return (pts[i][0] + (pts[i + 1][0] - pts[i][0]) * t,
                    pts[i][1] + (pts[i + 1][1] - pts[i][1]) * t)
        want -= L
    return pts[-1]

for lane in LANES:
    for f in (0.10, 0.22, 0.38, 0.62, 0.78, 0.90):
        x, z = path_point(lane, f)
        disc(x, z, 3.6, (140, 124, 92))
        d.ellipse([px(x - 3.6), py(z + 3.6), px(x + 3.6), py(z - 3.6)],
                  outline=(66, 54, 36), width=5)

# ---------- base plazas ----------
disc(-50, -50, 10.5, (146, 114, 44)); disc(-50, -50, 8.0, (168, 134, 58))
disc(50, 50, 10.5, (120, 133, 148)); disc(50, 50, 8.0, (152, 166, 182))

# ---------- the ancient lairs ----------
for (x, z) in [(-27, 12), (27, -12)]:
    disc(x, z, 7.0, (43, 34, 24))
    d.ellipse([px(x - 7), py(z + 7), px(x + 7), py(z - 7)],
              outline=(24, 19, 14), width=6)

# ---------- weathering: dirt freckles across the whole field ----------
for _ in range(900):
    x, z = rng.uniform(-60, 60, 2)
    r = rng.uniform(0.5, 2.2)
    tone = (94, 76, 50, 36) if rng.random() < 0.6 else (140, 120, 88, 30)
    d.ellipse([px(x - r), py(z + r), px(x + r), py(z - r)], fill=tone)

# ---------- grain + vignette ----------
arr = np.asarray(img).astype(np.int16)
noise = rng.normal(0, 7, (W, W, 1))
arr = np.clip(arr + noise, 0, 255).astype(np.uint8)
img = Image.fromarray(arr).convert("RGB")
shade = Image.new("L", (W, W), 0)
sd = ImageDraw.Draw(shade)
sd.ellipse([-W * 0.25, -W * 0.25, W * 1.25, W * 1.25], fill=255)
shade = shade.filter(ImageFilter.GaussianBlur(180))
dark = Image.new("RGB", (W, W), (30, 24, 16))
img = Image.composite(img, dark, shade)

img.save("/home/hatch/workspace/your_files/arena-of-kemet-unity/GroundPaint.jpg",
         quality=88)
print("GroundPaint.jpg painted")
