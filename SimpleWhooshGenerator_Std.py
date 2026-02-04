import math
import struct
import random

def generate_whoosh_wav(filename, duration=0.3, sample_rate=44100):
    num_samples = int(duration * sample_rate)
    max_amplitude = 32767 * 0.8

    # WAV Header
    header = bytearray()
    header.extend(b'RIFF')
    header.extend(struct.pack('<I', 36 + num_samples * 2))
    header.extend(b'WAVEfmt ')
    header.extend(struct.pack('<I', 16)) # Subchunk1Size
    header.extend(struct.pack('<H', 1))  # AudioFormat (PCM)
    header.extend(struct.pack('<H', 1))  # NumChannels (Mono)
    header.extend(struct.pack('<I', sample_rate)) # SampleRate
    header.extend(struct.pack('<I', sample_rate * 2)) # ByteRate
    header.extend(struct.pack('<H', 2))  # BlockAlign
    header.extend(struct.pack('<H', 16)) # BitsPerSample
    header.extend(b'data')
    header.extend(struct.pack('<I', num_samples * 2))

    data = bytearray()

    for i in range(num_samples):
        t = i / sample_rate

        # White noise
        noise = random.uniform(-1, 1)

        # Envelope: Attack (fast) -> Decay (fast)
        # Peak at 0.1s
        if t < 0.1:
            env = t / 0.1
        else:
            env = math.exp(-15 * (t - 0.1))

        value = int(noise * env * max_amplitude)
        data.extend(struct.pack('<h', value))

    with open(filename, 'wb') as f:
        f.write(header)
        f.write(data)

    print(f"Generated {filename}")

if __name__ == "__main__":
    generate_whoosh_wav("Assets/Resources/Music/SwordWhoosh.wav")
