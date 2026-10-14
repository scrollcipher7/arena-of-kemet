import numpy as np
from scipy.signal import lfilter

SR = 22050
BPM = 80.0
BEAT = 60.0 / BPM            # 0.75 s
BARS = 16
DUR = BARS * 4 * BEAT        # 48 s
N = int(DUR * SR)
rng = np.random.default_rng(7)

def onepole(x, a):
    return lfilter([a], [1.0, -(1 - a)], x)

# ---------- the drone: two roots breathing under everything ----------
t = np.arange(N) / SR
drone = (0.50 * np.sin(2 * np.pi * 73.42 * t) +          # D2
         0.30 * np.sin(2 * np.pi * 110.00 * t) +          # A2
         0.22 * np.sin(2 * np.pi * 146.83 * t) +          # D3
         0.10 * np.sin(2 * np.pi * 220.00 * t))
trem = 0.85 + 0.15 * np.sin(2 * np.pi * 0.07 * t)
drone = onepole(drone * trem, 0.02) * 0.5

# ---------- war drums ----------
def boom(buf, at, vol=1.0, f0=64, f1=38):
    i = int(at * SR); L = min(int(0.55 * SR), len(buf) - i)
    if L <= 0: return
    tt = np.arange(L) / SR
    freq = f1 + (f0 - f1) * np.exp(-tt * 9)
    ph = np.cumsum(freq) / SR
    env = np.exp(-tt * 6.5)
    buf[i:i + L] += vol * np.sin(2 * np.pi * ph) * env
    nz = rng.standard_normal(L) * np.exp(-tt * 40)
    buf[i:i + L] += vol * 0.25 * nz

def tek(buf, at, vol=0.5):
    i = int(at * SR); L = min(int(0.09 * SR), len(buf) - i)
    if L <= 0: return
    tt = np.arange(L) / SR
    nz = rng.standard_normal(L) * np.exp(-tt * 60)
    nz = nz - onepole(nz, 0.35)          # crude highpass snap
    buf[i:i + L] += vol * nz

drums = np.zeros(N)
for bar in range(BARS):
    b0 = bar * 4 * BEAT
    boom(drums, b0, 1.0)
    boom(drums, b0 + 2 * BEAT, 0.9)
    tek(drums, b0 + 2.5 * BEAT, 0.5)
    boom(drums, b0 + 3 * BEAT, 0.75, 58, 36)
    tek(drums, b0 + 3.5 * BEAT, 0.4)
    if bar % 4 == 3:
        boom(drums, b0 + 3.75 * BEAT, 0.6, 70, 40)
    if bar % 8 == 7:
        for k in range(4):
            tek(drums, b0 + k * BEAT, 0.35)

# ---------- the plucked lament (Karplus-Strong, D phrygian dominant) ----------
def pluck(freq, dur=1.6):
    n = int(SR / freq)
    buf = rng.standard_normal(n)
    out = np.zeros(int(dur * SR))
    line = buf.copy()
    idx = 0
    for i in range(len(out)):
        cur = line[idx]
        nxt = line[(idx + 1) % n]
        val = 0.996 * 0.5 * (cur + nxt)
        line[idx] = val
        out[i] = cur
        idx = (idx + 1) % n
    return out

NOTES = {"D4": 293.66, "Eb4": 311.13, "F#4": 369.99, "G4": 392.0,
         "A4": 440.0, "Bb4": 466.16, "C5": 523.25, "D5": 587.33}
PHRASE = [(2, 0, "D5"), (2, 1, "A4"), (2, 2, "Bb4"), (2, 3, "F#4"),
          (6, 0, "G4"), (6, 1, "Eb4"), (6, 2, "D4"),
          (10, 0, "D5"), (10, 1, "C5"), (10, 2, "Bb4"), (10, 3, "A4"),
          (14, 0, "Bb4"), (14, 2, "F#4"), (14, 3, "D4")]
melody = np.zeros(N)
for bar, beat, name in PHRASE:
    at = int((bar * 4 + beat) * BEAT * SR)
    p = pluck(NOTES[name]) * 0.55
    melody[at:at + len(p)] += p

# ---------- simple temple reverb on the melody ----------
def comb(x, delay, g):
    b = [1.0]; a = np.zeros(delay + 1); a[0] = 1.0; a[delay] = -g
    return lfilter(b, a, x)

wet = np.zeros(N)
for d, g in [(1557, 0.55), (1617, 0.52), (1491, 0.50), (1422, 0.48)]:
    wet += comb(melody, d, g)
melody = melody + wet * 0.35

# ---------- wind over the river ----------
wind = onepole(rng.standard_normal(N), 0.008)
wind *= (0.5 + 0.5 * np.sin(2 * np.pi * 0.045 * t)) * 0.16

mix = drone * 0.9 + drums * 0.85 + melody * 0.8 + wind
mix = np.tanh(mix * 1.1)

# seamless loop: fold the last 1.5 s into the first
X = int(1.5 * SR)
mix[:X] = mix[:X] + mix[N - X:] * np.linspace(1, 0, X)
mix = mix[:N - X]
mix = mix / np.max(np.abs(mix)) * 0.89

import wave
pcm = (np.clip(mix, -1, 1) * 32767).astype(np.int16)
with wave.open("/tmp/theme.wav", "wb") as w:
    w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
    w.writeframes(pcm.tobytes())
print("theme seconds:", len(mix) / SR)
