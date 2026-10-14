import numpy as np
from scipy.signal import lfilter
import wave, os

SR = 22050
rng = np.random.default_rng(11)
OUT = os.path.expanduser("~/workspace/your_files/arena-of-kemet-unity/audio/Sfx")
os.makedirs(OUT, exist_ok=True)

def save(name, x):
    x = x / (np.max(np.abs(x)) + 1e-9) * 0.89
    pcm = (np.clip(x, -1, 1) * 32767).astype(np.int16)
    with wave.open(f"/tmp/{name}.wav", "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes(pcm.tobytes())

def onepole(x, a):
    return lfilter([a], [1.0, -(1 - a)], x)

def pluck(freq, dur, damp=0.996):
    n = max(2, int(SR / freq))
    line = rng.standard_normal(n)
    out = np.zeros(int(dur * SR))
    idx = 0
    for i in range(len(out)):
        cur = line[idx]
        line[idx] = damp * 0.5 * (cur + line[(idx + 1) % n])
        out[i] = cur
        idx = (idx + 1) % n
    return out

# BOW — string release: low pluck + the snap of the string leaving fingers
d = 0.30
bow = pluck(150, d, 0.994) * 0.9
snap_n = int(0.03 * SR)
bow[:snap_n] += rng.standard_normal(snap_n) * np.exp(-np.arange(snap_n) / (0.008 * SR)) * 0.7
save("bow", bow * np.exp(-np.arange(len(bow)) / (0.16 * SR)) + bow * 0.0 + pluck(150, d, 0.994) * 0.001)

# MAGIC — a falling star of a bolt: sine sweep down + shimmering breath
d = 0.32; tt = np.arange(int(d * SR)) / SR
f = 240 + 700 * np.exp(-tt * 9)
ph = np.cumsum(f) / SR
magic = np.sin(2 * np.pi * ph) * np.exp(-tt * 7) * 0.8
magic += onepole(rng.standard_normal(len(tt)), 0.25) * np.exp(-tt * 10) * 0.35
save("magic", magic)

# TOWER — the heavy stone-bow: deep pluck + a boom in the tail
d = 0.5; tt = np.arange(int(d * SR)) / SR
tower = pluck(78, d, 0.997) * 1.0
f2 = 34 + 26 * np.exp(-tt * 8)
ph2 = np.cumsum(f2) / SR
tower += np.sin(2 * np.pi * ph2) * np.exp(-tt * 5.5) * 0.7
save("tower", tower)

# HIT — flesh and wood struck: short thump + click
d = 0.12; tt = np.arange(int(d * SR)) / SR
hit = np.sin(2 * np.pi * 170 * tt) * np.exp(-tt * 45) * 0.9
hit += onepole(rng.standard_normal(len(tt)), 0.4) * np.exp(-tt * 90) * 0.6
save("hit", hit)
print("sfx rendered")
