import numpy as np
from PIL import Image, ImageDraw, ImageFilter

# THE EARTH OF KEMET — the Two Lands painted as one field:
# DESHRET, the Red Land (desert gold) everywhere, and KEMET, the
# Black Land — the dark fertile soil and green cultivation that hug
# the Nile. The roads are processional sandstone, slab-seamed.
W = 2048
S = W / 120.0                     # pixels per world unit; world spans -60..60
rng = np.random.default_rng(42)

def px(x): return (x + 60) * S
def py(z): return (60 - z) * S    # image top = +Z (north rim)

def P(x, z): return (px(x), py(z))

# ---------- base earth: DESHRET, the red-gold desert ----------
img = Image.new("RGBA", (W, W), (178, 142, 84, 255))
d = ImageDraw.Draw(img, "RGBA")
for _ in range(500):
    x, z = rng.uniform(-62, 62, 2)
    r = rng.uniform(4, 16)
    _shades = [(196, 164, 102), (160, 124, 70), (188, 152, 92), (146, 112, 62)]
    shade = _shades[int(rng.integers(len(_shades)))]
    d.ellipse([px(x - r), py(z + r), px(x + r), py(z - r)], fill=shade + (36,))
img = img.filter(ImageFilter.GaussianBlur(18))
d = ImageDraw.Draw(img, "RGBA")

# ---------- palm-grove shade (the oases between the roads) ----------
for _ in range(200):
    x, z = rng.uniform(-58, 58, 2)
    r = rng.uniform(2.5, 7)
    d.ellipse([px(x - r), py(z + r), px(x + r), py(z - r)],
              fill=(96, 104, 52, 28))
for _ in range(120):
    x, z = rng.uniform(-58, 58, 2)
    r = rng.uniform(1.0, 2.6)
    d.ellipse([px(x - r), py(z + r), px(x + r), py(z - r)],
              fill=(58, 66, 34, 32))
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

# ---------- THE NILE: black land, green fields, then the water ----------
stroke([(-24, 24), (24, -24)], 13.0, (44, 40, 30))            # the Black Land
disc(-24, 24, 16.5, (44, 40, 30)); disc(24, -24, 16.5, (44, 40, 30))
stroke([(-24, 24), (24, -24)], 10.0, (74, 96, 46))            # the cultivation
disc(-24, 24, 14.2, (74, 96, 46)); disc(24, -24, 14.2, (74, 96, 46))
stroke([(-24, 24), (24, -24)], 6.8, (22, 88, 116))            # the river deep
disc(-24, 24, 13.0, (22, 88, 116)); disc(24, -24, 13.0, (22, 88, 116))
stroke([(-24, 24), (24, -24)], 3.2, (40, 128, 158))           # the bright channel
disc(-24, 24, 8.5, (40, 128, 158)); disc(24, -24, 8.5, (40, 128, 158))

# ---------- the roads: processional sandstone ----------
for lane in LANES:
    stroke(lane, 8.6, (110, 84, 48))         # the dark seam-edge
    stroke(lane, 7.2, (208, 182, 128))       # the sandstone
    stroke(lane, 5.6, (192, 164, 108))       # the worn heart of the road

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

# slab seams across the roads, like cut temple stone
for lane in LANES:
    f = 0.02
    while f < 0.99:
        x, z = path_point(lane, f)
        x2, z2 = path_point(lane, min(f + 0.004, 1.0))
        dx, dz = x2 - x, z2 - z
        L = np.hypot(dx, dz) or 1.0
        nx, nz = -dz / L * 3.1, dx / L * 3.1
        d.line([P(x - nx, z - nz), P(x + nx, z + nz)],
               fill=(110, 84, 48, 110), width=3)
        f += 0.028

# tower pads at the march fractions both courts build on
for lane in LANES:
    for f in (0.10, 0.22, 0.38, 0.62, 0.78, 0.90):
        x, z = path_point(lane, f)
        disc(x, z, 3.6, (176, 148, 96))
        d.ellipse([px(x - 3.6), py(z + 3.6), px(x + 3.6), py(z - 3.6)],
                  outline=(110, 84, 48), width=5)

# ---------- base plazas: the sun of Kemet, the marble of Olympus ----------
disc(-50, -50, 10.5, (158, 120, 52)); disc(-50, -50, 8.0, (196, 156, 74))
disc(-50, -50, 3.4, (232, 192, 92))
d.ellipse([px(-53.4), py(-46.6), px(-46.6), py(-53.4)], outline=(146, 110, 44), width=5)
disc(50, 50, 10.5, (138, 150, 166)); disc(50, 50, 8.0, (178, 188, 202))
disc(50, 50, 3.4, (208, 216, 228))
d.ellipse([px(46.6), py(53.4), px(53.4), py(46.6)], outline=(120, 132, 150), width=5)

# ---------- the ancient lairs: basalt, ringed in old gold ----------
for (x, z) in [(-27, 12), (27, -12)]:
    disc(x, z, 7.0, (46, 44, 50))
    d.ellipse([px(x - 7), py(z + 7), px(x + 7), py(z - 7)],
              outline=(150, 120, 58), width=5)

# ---------- weathering: sand freckles and pale stones ----------
for _ in range(650):
    x, z = rng.uniform(-60, 60, 2)
    r = rng.uniform(0.3, 1.1)
    tone = (120, 96, 58, 22) if rng.random() < 0.6 else (206, 178, 120, 20)
    d.ellipse([px(x - r), py(z + r), px(x + r), py(z - r)], fill=tone)

# ---------- grain + warm vignette ----------
arr = np.asarray(img).astype(np.int16)
noise = rng.normal(0, 7, (W, W, 1))
arr = np.clip(arr + noise, 0, 255).astype(np.uint8)
img = Image.fromarray(arr).convert("RGB")
shade = Image.new("L", (W, W), 0)
sd = ImageDraw.Draw(shade)
sd.ellipse([-W * 0.25, -W * 0.25, W * 1.25, W * 1.25], fill=255)
shade = shade.filter(ImageFilter.GaussianBlur(180))
dark = Image.new("RGB", (W, W), (34, 24, 12))
img = Image.composite(img, dark, shade)

img.save("/home/hatch/workspace/your_files/arena-of-kemet-unity/GroundPaint.jpg",
         quality=88)
print("GroundPaint.jpg painted — the Two Lands")
