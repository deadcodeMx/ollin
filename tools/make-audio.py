import math
import os
import struct
import wave

RATE = 44100
OUT = os.path.join(os.getcwd(), "assets", "audio")
os.makedirs(OUT, exist_ok=True)


def write(name, samples):
    path = os.path.join(OUT, name)
    with wave.open(path, "w") as f:
        f.setnchannels(1)
        f.setsampwidth(2)
        f.setframerate(RATE)
        data = b"".join(struct.pack("<h", max(-32767, min(32767, int(s * 32767)))) for s in samples)
        f.writeframes(data)
    print(name, len(samples) / RATE, "s")


def envelope(t, duration, attack=0.008, release=0.04):
    if t < attack:
        return t / attack
    if t > duration - release:
        return max(0.0, (duration - t) / release)
    return 1.0


# Salto: barrido ascendente
dur = 0.2
jump = []
for i in range(int(RATE * dur)):
    t = i / RATE
    freq = 320 + 480 * (t / dur)
    s = math.sin(2 * math.pi * freq * t) * 0.5 * envelope(t, dur)
    jump.append(s)
write("jump.wav", jump)

# Moneda: dos notas cortas
dur = 0.18
coin = []
for i in range(int(RATE * dur)):
    t = i / RATE
    freq = 988 if t < 0.07 else 1319
    s = math.sin(2 * math.pi * freq * t) * 0.45 * envelope(t, dur)
    coin.append(s)
write("coin.wav", coin)

# Golpe: ruido con caída rápida
dur = 0.12
hit = []
seed = 12345
for i in range(int(RATE * dur)):
    t = i / RATE
    seed = (1103515245 * seed + 12345) & 0x7FFFFFFF
    noise = (seed / 0x7FFFFFFF) * 2 - 1
    s = noise * 0.5 * math.exp(-t * 32)
    hit.append(s)
write("hit.wav", hit)

# Ambiente: drone grave en bucle de 4 s
dur = 4.0
amb = []
for i in range(int(RATE * dur)):
    t = i / RATE
    tremolo = 0.75 + 0.25 * math.sin(2 * math.pi * 0.25 * t)
    s = (math.sin(2 * math.pi * 55 * t) * 0.35 + math.sin(2 * math.pi * 82.5 * t) * 0.2) * tremolo
    s += math.sin(2 * math.pi * 110 * t) * 0.05
    amb.append(s)
write("ambient.wav", amb)
