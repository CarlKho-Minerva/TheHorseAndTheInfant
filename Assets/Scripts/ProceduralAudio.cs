using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Procedural Audio Engine - ENHANCED
/// Features: Multiple Sound Types, Stereo Panning, EQ Simulation, Runtime Generation
/// </summary>
public class ProceduralAudio : MonoBehaviour
{
    public enum SoundType { Whoosh, Impact, Heartbeat, Rumble, Slice }

    [System.Serializable]
    public class SoundProfile
    {
        public SoundType type;
        public float duration = 0.3f;
        [Range(0.1f, 30f)] public float decayRate = 10f;
        [Range(20f, 2000f)] public float basePitch = 440f;
        [Range(0f, 1f)] public float noiseAmount = 1f;
        [Range(0f, 1f)] public float toneAmount = 0f;
        public bool useLowPass = false;
        [Range(100f, 5000f)] public float lowPassCutoff = 1000f;
    }

    [Header("Audio Settings")]
    public int sampleRate = 44100;
    public List<SoundProfile> soundProfiles = new List<SoundProfile>();

    [Header("Variation")]
    [Range(0f, 0.5f)] public float pitchVariation = 0.2f;
    [Range(0f, 1f)] public float panVariation = 0.3f;

    [Header("Debug")]
    public bool logGeneration = false;

    private AudioSource audioSource;
    private Dictionary<SoundType, AudioClip> generatedClips = new Dictionary<SoundType, AudioClip>();

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // Pre-generate default sounds
        InitializeDefaultProfiles();
        RegenerateAllClips();
    }

    void InitializeDefaultProfiles()
    {
        if (soundProfiles.Count == 0)
        {
            // Whoosh - Fast sword swing
            soundProfiles.Add(new SoundProfile
            {
                type = SoundType.Whoosh,
                duration = 0.25f,
                decayRate = 15f,
                noiseAmount = 1f,
                toneAmount = 0f
            });

            // Impact - Heavy hit
            soundProfiles.Add(new SoundProfile
            {
                type = SoundType.Impact,
                duration = 0.4f,
                decayRate = 8f,
                noiseAmount = 0.7f,
                toneAmount = 0.3f,
                basePitch = 80f,
                useLowPass = true,
                lowPassCutoff = 500f
            });

            // Heartbeat - Thump
            soundProfiles.Add(new SoundProfile
            {
                type = SoundType.Heartbeat,
                duration = 0.6f,
                decayRate = 5f,
                noiseAmount = 0.2f,
                toneAmount = 0.8f,
                basePitch = 60f,
                useLowPass = true,
                lowPassCutoff = 200f
            });

            // Slice - Sharp cut
            soundProfiles.Add(new SoundProfile
            {
                type = SoundType.Slice,
                duration = 0.15f,
                decayRate = 25f,
                noiseAmount = 0.8f,
                toneAmount = 0.2f,
                basePitch = 1200f
            });
        }
    }

    public void RegenerateAllClips()
    {
        generatedClips.Clear();
        foreach (var profile in soundProfiles)
        {
            generatedClips[profile.type] = GenerateClip(profile);
            if (logGeneration) Debug.Log($"[ProceduralAudio] Generated: {profile.type}");
        }
    }

    /// <summary>
    /// Play a specific sound type with optional pitch and pan overrides
    /// </summary>
    public void PlaySound(SoundType type, float pitchOverride = -1f, float panOverride = 0f)
    {
        if (!generatedClips.ContainsKey(type))
        {
            Debug.LogWarning($"[ProceduralAudio] Sound type {type} not generated!");
            return;
        }

        // Apply variation
        float pitch = pitchOverride > 0 ? pitchOverride : 1f + Random.Range(-pitchVariation, pitchVariation);
        float pan = Mathf.Clamp(panOverride + Random.Range(-panVariation, panVariation), -1f, 1f);

        audioSource.pitch = pitch;
        audioSource.panStereo = pan;
        audioSource.PlayOneShot(generatedClips[type]);
    }

    // Convenience methods
    public void PlayWhoosh() => PlaySound(SoundType.Whoosh);
    public void PlayImpact() => PlaySound(SoundType.Impact, 0.7f);
    public void PlayHeartbeat() => PlaySound(SoundType.Heartbeat, 1f);
    public void PlaySlice() => PlaySound(SoundType.Slice, Random.Range(1.0f, 1.3f));

    /// <summary>
    /// Play a quick "combo" burst (for multi-hit)
    /// </summary>
    public void PlayCombo(int hits)
    {
        for (int i = 0; i < hits; i++)
        {
            float pitch = 1f + (i * 0.1f); // Each hit gets slightly higher
            Invoke(nameof(PlaySlice), i * 0.08f); // Stagger
        }
    }

    private AudioClip GenerateClip(SoundProfile profile)
    {
        int sampleCount = (int)(sampleRate * profile.duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;

            // Noise component
            float noise = Random.Range(-1f, 1f) * profile.noiseAmount;

            // Tone component (sine wave)
            float tone = Mathf.Sin(2 * Mathf.PI * profile.basePitch * t) * profile.toneAmount;

            // Combined signal
            float signal = noise + tone;

            // Envelope
            float envelope = Mathf.Exp(-profile.decayRate * t);
            signal *= envelope;

            // Simple low-pass approximation (running average)
            if (profile.useLowPass && i > 0)
            {
                float alpha = profile.lowPassCutoff / sampleRate;
                samples[i] = samples[i - 1] + alpha * (signal - samples[i - 1]);
            }
            else
            {
                samples[i] = signal;
            }
        }

        // Normalize
        float max = 0f;
        foreach (var s in samples) max = Mathf.Max(max, Mathf.Abs(s));
        if (max > 0) for (int i = 0; i < samples.Length; i++) samples[i] /= max;

        AudioClip clip = AudioClip.Create($"Proc_{profile.type}", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
