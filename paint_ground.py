import numpy as np, math, random
from PIL import Image, ImageDraw, ImageFilter

# THE VISION PAINT — the land as the maker's picture shows it:
# clean DESERT SAND over everything, processional roads of pale stone
# with hieroglyphs carved along their edges, the Nile bright between
# sandy banks, palm ponds in the wild ground.
W = 2048
S = W / 120.0
rng = np.random.default_rng(11)
random.seed(11)

def px(x): return (x + 60) * S
def py(z): return (60 - z) * S
def P(x, z): return (px(x), py(z))

img = Image.new("RGBA", (W, W), (208, 180, 122, 255))
d = ImageDraw.Draw(img, "RGBA")
for _ in range(420):                       # soft sand drift
    x, z = rng.uniform(-62, 62, 2); r = rng.uniform(5, 18)
    shade = [(220, 194, 138), (196, 166, 106), (214, 186, 128)][int(rng.integers(3))]
    d.ellipse([px(x-r), py(z+r), px(x+r), py(z-r)], fill=shade + (34,))
img = img.filter(ImageFilter.GaussianBlur(16)); d = ImageDraw.Draw(img, "RGBA")

LANES = [
    [(-50, -50), (-50, 38), (-38, 50), (38, 50), (50, 38), (50, 50)],
    [(-50, -50), (0, 0), (50, 50)],
    [(-50, -50), (-38, -50), (38, -50), (50, -38), (50, 50)],
]
def stroke(pts, wu, color):
    d.line([P(x, z) for x, z in pts], fill=color, width=int(wu * S), joint="curve")
def disc(x, z, r, color):
    d.ellipse([px(x-r), py(z+r), px(x+r), py(z-r)], fill=color)
def path_point(pts, frac):
    segs, total = [], 0.0
    for i in range(len(pts) - 1):
        L = np.hypot(pts[i+1][0]-pts[i][0], pts[i+1][1]-pts[i][1]); segs.append(L); total += L
    want = frac * total
    for i, L in enumerate(segs):
        if want <= L:
            t = want / L if L > 0 else 0
            return (pts[i][0] + (pts[i+1][0]-pts[i][0]) * t, pts[i][1] + (pts[i+1][1]-pts[i][1]) * t)
        want -= L
    return pts[-1]

# ---------- the Nile: sand banks, bright water ----------
stroke([(-24, 24), (24, -24)], 9.6, (188, 158, 104))
disc(-24, 24, 15.0, (188, 158, 104)); disc(24, -24, 15.0, (188, 158, 104))
stroke([(-24, 24), (24, -24)], 6.4, (30, 108, 142))
disc(-24, 24, 12.6, (30, 108, 142)); disc(24, -24, 12.6, (30, 108, 142))
stroke([(-24, 24), (24, -24)], 3.0, (52, 140, 172))
disc(-24, 24, 8.2, (52, 140, 172)); disc(24, -24, 8.2, (52, 140, 172))
# reed rims on the pools
for (cx, cz) in [(-24, 24), (24, -24)]:
    for _ in range(46):
        a = random.uniform(0, 6.283); rr = random.uniform(11.5, 13.5)
        x, z = cx + rr*math.cos(a), cz + rr*math.sin(a)
        disc(x, z, random.uniform(0.5, 1.1), (84, 104, 52))

# ---------- the roads: pale processional stone ----------
for lane in LANES:
    stroke(lane, 8.8, (146, 116, 70))
    stroke(lane, 7.4, (224, 202, 154))
    stroke(lane, 5.8, (214, 190, 138))

# ---------- hieroglyphs carved along the road edges ----------
def glyph(kind, x, y, s, col):
    if kind == 0: d.ellipse([x-s, y-s*0.55, x+s, y+s*0.55], outline=col, width=3); d.ellipse([x-s*0.3, y-s*0.3, x+s*0.3, y+s*0.3], fill=col)
    elif kind == 1:
        d.ellipse([x-s*0.35, y-s, x+s*0.35, y-s*0.3], outline=col, width=3); d.line([(x, y-s*0.3), (x, y+s)], fill=col, width=3); d.line([(x-s*0.6, y+s*0.1), (x+s*0.6, y+s*0.1)], fill=col, width=3)
    elif kind == 2: d.ellipse([x-s*0.6, y-s*0.6, x+s*0.6, y+s*0.6], outline=col, width=3)
    elif kind == 3:
        for k in range(3):
            yy = y - s*0.5 + k*s*0.45
            d.arc([x-s, yy-s*0.22, x, yy+s*0.22], 0, 180, fill=col, width=3); d.arc([x, yy-s*0.22, x+s, yy+s*0.22], 180, 360, fill=col, width=3)
    elif kind == 4: d.line([(x, y+s), (x-s*0.4, y-s)], fill=col, width=3); d.line([(x, y+s), (x+s*0.4, y-s)], fill=col, width=3)
    elif kind == 5:
        for a in range(5):
            x2 = x + s*0.8*math.cos(-1.57 + a*1.2566); y2 = y + s*0.8*math.sin(-1.57 + a*1.2566)
            d.line([(x, y), (x2, y2)], fill=col, width=3)
    elif kind == 6:
        pts = [(x + s*0.7*math.sin(t/4.0), y - s + t*(2*s/20.0)) for t in range(21)]; d.line(pts, fill=col, width=3)
    else:
        d.ellipse([x-s*0.6, y-s*0.5, x+s*0.6, y+s*0.7], outline=col, width=3); d.line([(x, y-s*0.5), (x, y+s*0.7)], fill=col, width=3)
carve = (120, 94, 56, 200)
for lane in LANES:
    f = 0.012
    while f < 0.995:
        x, z = path_point(lane, f); x2, z2 = path_point(lane, min(f + 0.004, 1.0))
        dx, dz = x2 - x, z2 - z; L = np.hypot(dx, dz) or 1.0
        nx, nz = -dz / L, dx / L
        for side in (-2.9, 2.9):
            gx, gz = x + nx * side, z + nz * side
            glyph(random.randrange(8), px(gx), py(gz), 13, carve)
        f += 0.021

# tower pads (obelisk ground)
for lane in LANES:
    for f in (0.10, 0.22, 0.38, 0.62, 0.78, 0.90):
        x, z = path_point(lane, f)
        disc(x, z, 3.9, (196, 168, 112))
        d.ellipse([px(x-3.9), py(z+3.9), px(x+3.9), py(z-3.9)], outline=(146, 116, 70), width=5)

# ---------- plazas: sun of Kemet, marble of Olympus ----------
disc(-50, -50, 10.5, (168, 128, 56)); disc(-50, -50, 8.0, (204, 164, 80)); disc(-50, -50, 3.4, (238, 200, 96))
d.ellipse([px(-53.4), py(-46.6), px(-46.6), py(-53.4)], outline=(150, 114, 46), width=5)
disc(50, 50, 10.5, (140, 152, 168)); disc(50, 50, 8.0, (182, 192, 206)); disc(50, 50, 3.4, (212, 220, 232))
d.ellipse([px(46.6), py(53.4), px(53.4), py(46.6)], outline=(122, 134, 152), width=5)

# ---------- lairs: basalt with gold ring ----------
for (x, z) in [(-27, 12), (27, -12)]:
    disc(x, z, 7.0, (48, 46, 52))
    d.ellipse([px(x-7), py(z+7), px(x+7), py(z-7)], outline=(150, 120, 58), width=5)

# ---------- wild ground: rock speckle + sparse palm shade ----------
for _ in range(200):
    x, z = rng.uniform(-59, 59, 2); r = rng.uniform(0.3, 1.3)
    tone = (150, 128, 88, 15)
    d.ellipse([px(x-r), py(z+r), px(x+r), py(z-r)], fill=tone)
for _ in range(110):
    x, z = rng.uniform(-58, 58, 2); r = rng.uniform(0.5, 1.2)
    d.ellipse([px(x-r), py(z+r), px(x+r), py(z-r)], fill=(104, 112, 58, 13))
img = img.filter(ImageFilter.GaussianBlur(1.2)); d = ImageDraw.Draw(img, "RGBA")

arr = np.asarray(img).astype(np.int16)
noise = rng.normal(0, 6, (W, W, 1))
arr = np.clip(arr + noise, 0, 255).astype(np.uint8)
img = Image.fromarray(arr).convert("RGB")
shade = Image.new("L", (W, W), 0); sd = ImageDraw.Draw(shade)
sd.ellipse([-W*0.25, -W*0.25, W*1.25, W*1.25], fill=255)
shade = shade.filter(ImageFilter.GaussianBlur(180))
img = Image.composite(img, Image.new("RGB", (W, W), (36, 26, 14)), shade)
img.save("/home/hatch/workspace/your_files/arena-of-kemet-unity/GroundPaint.jpg", quality=88)
print("GroundPaint v3 — THE VISION land")
