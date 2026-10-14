import numpy as np
from PIL import Image
import os

BASE = os.path.expanduser("~/workspace/your_files/arena-of-kemet-unity/bodies/kaykit/KayKit-Game-Assets-KayKit-Character-Pack-Adventures-1.0-672074b/addons/kaykit_character_pack_adventures/Characters/gltf")
OUT = os.path.expanduser("~/workspace/your_files/arena-of-kemet-unity/bodies/textures")
os.makedirs(OUT, exist_ok=True)

SKIN = [(200, 133, 95), (246, 192, 156), (248, 204, 171)]
LEATHER = [(125, 61, 44), (155, 90, 69), (178, 112, 82)]
CLOTH = [(60, 66, 70), (83, 71, 65), (89, 96, 100), (109, 96, 92), (115, 99, 87),
         (121, 131, 136), (131, 114, 101), (151, 143, 134), (168, 162, 157), (170, 184, 190)]
BLUE = [(77, 107, 130)]
GREEN = [(12, 95, 76), (15, 131, 69)]
RTRIM = [(34, 137, 147), (200, 218, 82), (255, 234, 37)]
TEAL = [(47, 108, 115)]
MTRIM = [(179, 87, 41)]
MBLUE = [(77, 107, 130)]

def shade3(triplet):
    """Map a family's ordered anchors onto a dark/mid/light triplet, keeping steps."""
    return triplet

def family_map(anchors, triplet):
    dark, mid, light = triplet
    n = len(anchors)
    out = {}
    for i, a in enumerate(anchors):
        t = i / max(1, n - 1)
        if t < 0.5:
            c = tuple(round(dark[k] + (mid[k] - dark[k]) * (t * 2)) for k in range(3))
        else:
            c = tuple(round(mid[k] + (light[k] - mid[k]) * ((t - 0.5) * 2)) for k in range(3))
        out[a] = c
    return out

GODS = {
    # name: (atlas, {family: triplet}, skin_triplet or None)
    "Medjay":  ("rogue_texture.png",     {"CLOTH": ((84, 58, 28), (140, 100, 48), (198, 154, 86)),
                                          "LEATHER": ((92, 60, 34), (122, 82, 48), (152, 106, 64)),
                                          "GREEN": ((110, 76, 32), (150, 108, 44), (196, 148, 74)),
                                          "RTRIM": ((190, 142, 58), (216, 170, 70), (232, 196, 96))}, None),
    "Artemis": ("rogue_texture.png",     {"CLOTH": ((40, 60, 34), (72, 100, 58), (118, 144, 98)),
                                          "LEATHER": ((86, 58, 36), (112, 78, 50), (140, 100, 66)),
                                          "GREEN": ((20, 66, 34), (28, 98, 48), (46, 130, 68)),
                                          "RTRIM": ((140, 170, 140), (170, 200, 170), (200, 220, 190))}, None),
    "Anubis":  ("rogue_texture.png",     {"CLOTH": ((16, 14, 20), (30, 27, 38), (122, 97, 45)),
                                          "LEATHER": ((22, 19, 17), (38, 32, 29), (58, 49, 44)),
                                          "GREEN": ((12, 11, 14), (20, 18, 24), (32, 28, 38)),
                                          "RTRIM": ((150, 112, 44), (200, 156, 62), (232, 196, 96))},
                                          ((46, 39, 37), (56, 48, 46), (72, 62, 58))),
    "Hades":   ("rogue_texture.png",     {"CLOTH": ((28, 22, 42), (50, 40, 72), (86, 70, 110)),
                                          "LEATHER": ((30, 26, 38), (46, 40, 56), (66, 58, 78)),
                                          "GREEN": ((34, 26, 50), (50, 38, 72), (74, 58, 102)),
                                          "RTRIM": ((168, 158, 148), (196, 188, 176), (220, 214, 204))},
                                          ((148, 130, 122), (178, 162, 152), (198, 186, 176))),
    "Thoth":   ("mage_texture.png",      {"CLOTH": ((88, 140, 130), (160, 196, 186), (240, 242, 236)),
                                          "LEATHER": ((138, 100, 50), (172, 132, 66), (206, 170, 98)),
                                          "TEAL": ((46, 110, 100), (46, 110, 100), (46, 110, 100)),
                                          "MTRIM": ((196, 150, 66), (196, 150, 66), (196, 150, 66)),
                                          "MBLUE": ((58, 120, 112), (58, 120, 112), (58, 120, 112))}, None),
    "Zeus":    ("mage_texture.png",      {"CLOTH": ((40, 60, 112), (88, 124, 186), (238, 241, 247)),
                                          "LEATHER": ((118, 88, 50), (150, 116, 64), (184, 148, 88)),
                                          "TEAL": ((38, 66, 132), (38, 66, 132), (38, 66, 132)),
                                          "MTRIM": ((204, 164, 72), (204, 164, 72), (204, 164, 72)),
                                          "MBLUE": ((48, 84, 156), (48, 84, 156), (48, 84, 156))}, None),
    "Isis":    ("mage_texture.png",      {"CLOTH": ((60, 118, 80), (126, 176, 128), (243, 240, 226)),
                                          "LEATHER": ((138, 100, 50), (172, 132, 66), (206, 170, 98)),
                                          "TEAL": ((60, 134, 88), (60, 134, 88), (60, 134, 88)),
                                          "MTRIM": ((196, 150, 66), (196, 150, 66), (196, 150, 66)),
                                          "MBLUE": ((70, 140, 100), (70, 140, 100), (70, 140, 100))}, None),
    "Sekhmet": ("barbarian_texture.png", {"CLOTH": ((94, 38, 30), (138, 52, 38), (180, 92, 64)),
                                          "LEATHER": ((112, 48, 32), (146, 66, 42), (182, 96, 62)),
                                          "BLUE": ((120, 30, 24), (164, 44, 34), (192, 72, 52))}, None),
    "Ares":    ("barbarian_texture.png", {"CLOTH": ((50, 46, 46), (82, 74, 70), (118, 108, 100)),
                                          "LEATHER": ((56, 42, 34), (82, 62, 48), (110, 86, 64)),
                                          "BLUE": ((94, 16, 16), (128, 24, 24), (162, 42, 38))}, None),
    "Athena":  ("knight_texture.png",    {"LEATHER": ((206, 196, 178), (228, 220, 204), (240, 236, 226))}, None),
}
SWEEPS = {
    "Medjay":  {"green": "GREEN", "blue": "RTRIM"},
    "Artemis": {"green": "GREEN", "blue": "RTRIM"},
    "Anubis":  {"green": "GREEN", "blue": "RTRIM"},
    "Hades":   {"green": "GREEN", "blue": "RTRIM"},
    "Thoth":   {"blue": "CLOTH", "green": "TEAL"},
    "Zeus":    {"blue": "CLOTH", "green": "TEAL"},
    "Isis":    {"blue": "CLOTH", "green": "TEAL"},
    "Sekhmet": {"blue": "BLUE"},
    "Ares":    {"blue": "BLUE"},
    "Athena":  {},
}
FAMS = {"SKIN": SKIN, "LEATHER": LEATHER, "CLOTH": CLOTH, "BLUE": BLUE,
        "GREEN": GREEN, "RTRIM": RTRIM, "TEAL": TEAL, "MTRIM": MTRIM, "MBLUE": MBLUE}

def hue_class(px):
    r, g, b = px[..., 0], px[..., 1], px[..., 2]
    cls = np.full(r.shape, "", dtype=object)
    cls[(g > r + 25) & (g > b + 15)] = "green"
    cls[(b > r + 20) & (b >= g)] = "blue"
    return cls

def lerp3(trip, t):
    dark, mid, light = [np.array(c, dtype=float) for c in trip]
    t = t[..., None]
    low = dark + (mid - dark) * (t * 2)
    high = mid + (light - mid) * ((t - 0.5) * 2)
    return np.where(t < 0.5, low, high)

for god, (atlas, fams, skin) in GODS.items():
    orig = np.asarray(Image.open(os.path.join(BASE, atlas)).convert("RGB")).astype(int)
    img = orig.copy()
    h, w, _ = img.shape
    flat = img.reshape(-1, 3)
    mapping = {}
    for fam, trip in fams.items():
        mapping.update(family_map(FAMS[fam], trip))
    if skin is not None:
        mapping.update(family_map(SKIN, skin))
    for src, dst in mapping.items():
        d = np.abs(flat - np.array(src)).sum(axis=1)
        flat[d <= 30] = dst
    img = flat.reshape(h, w, 3)
    # Hue sweeps: the gradient islands, recolored by their own light.
    cls = hue_class(orig)
    vmax = orig.max(axis=2)
    tmap = np.clip((vmax - 70) / 110.0, 0, 1)
    for cls_name, fam_key in SWEEPS.get(god, {}).items():
        trip = fams.get(fam_key)
        if trip is None: continue
        mask = cls == cls_name
        if mask.any():
            img[mask] = lerp3(trip, tmap[mask]).astype(int)
    out = Image.fromarray(img.astype(np.uint8))
    out.save(os.path.join(OUT, god + ".png"))
    print("painted", god)
print("DONE")
