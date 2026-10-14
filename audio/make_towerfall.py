import numpy as np
from scipy.signal import lfilter
import wave

SR = 22050
rng = np.random.default_rng(23)

def onepole(x, a):
    return lfilter([a], [1.0, -(1 - a)], x)

D = 2.4
N = int(D * SR)
t = np.arange(N) / SR
out = np.zeros(N)

# The blast: a deep pitch-falling boom that shakes the dust loose
f = 26 + 48 * np.exp(-t * 5)
ph = np.cumsum(f) / SR
out += np.sin(2 * np.pi * ph) * np.exp(-t * 2.6) * 1.1
out += onepole(rng.standard_normal(N), 0.10) * np.exp(-t * 7) * 0.9   # the crack of stone

# The rubble: blocks striking ground, later and later, weaker and weaker
for k in range(26):
    at = 0.10 + (k / 26) ** 1.4 * 1.7 + rng.uniform(0, 0.05)
    i = int(at * SR); L = int(rng.uniform(0.05, 0.16) * SR)
    if i + L >= N: continue
    seg = onepole(rng.standard_normal(L), rng.uniform(0.15, 0.45))
    seg *= np.exp(-np.arange(L) / (0.03 * SR))
    out[i:i + L] += seg * rng.uniform(0.25, 0.65) * (1 - k / 34)

# The rumble that will not leave: low noise rolling to a stop
out += onepole(rng.standard_normal(N), 0.02) * np.exp(-t * 1.5) * 0.55

out = np.tanh(out * 1.15)
out = out / np.max(np.abs(out)) * 0.92
pcm = (np.clip(out, -1, 1) * 32767).astype(np.int16)
with wave.open("/tmp/towerfall.wav", "wb") as w:
    w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
    w.writeframes(pcm.tobytes())
print("towerfall rendered", D, "s")
