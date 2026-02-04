import numpy as np
from scipy.io.wavfile import write

def generate_whoosh(filename, duration=0.3, sample_rate=44100):
    t = np.linspace(0, duration, int(sample_rate * duration))

    # White noise
    noise = np.random.normal(0, 1, len(t))

    # Volume envelope (Attack, Sustain, Release)
    # Fast attack, fast decay to make it sharp
    envelope = np.exp(-15 * (t - 0.1)**2)

    # Basic Low Pass Filter effect simulation by smoothing random noise
    # (Actual LPF is complex, but moving average works for "softening" noise)
    # We want a "swipe" so we want the filter to open and close.
    # Instead of complex filtering, let's just use the envelope on the noise directly
    # and maybe mix in a sine wave for "whistling" air.

    # Pure noise whoosh
    audio = noise * envelope

    # Normalize
    audio = audio / np.max(np.abs(audio)) * 0.8

    # Convert to 16-bit PCM
    audio_int16 = (audio * 32767).astype(np.int16)

    write(filename, sample_rate, audio_int16)
    print(f"Generated {filename}")

generate_whoosh("Assets/Resources/Music/SwordWhoosh.wav")
