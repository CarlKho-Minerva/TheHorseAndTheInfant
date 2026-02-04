using UnityEngine;

/// <summary>
/// Flame Trail Enhancer - ENHANCED
/// Features: Combo-based Color, Dynamic Width, Emission Scaling, Afterimage Effect
/// </summary>
[RequireComponent(typeof(TrailRenderer))]
public class FlameTrailEnhancer : MonoBehaviour
{
    [Header("Base Settings")]
    public float baseWidth = 0.5f;
    [Range(0.1f, 0.5f)] public float trailTime = 0.25f;

    [Header("Combo Colors")]
    [Tooltip("Color progression as combo increases")]
    public Gradient combo1Gradient;
    public Gradient combo2Gradient;
    public Gradient combo3Gradient;

    [Header("Dynamic Scaling")]
    [Tooltip("Width multiplier at max combo")]
    public float maxWidthMultiplier = 1.5f;
    [Tooltip("Emission boost at max combo")]
    public float maxEmissionMultiplier = 3f;

    [Header("Afterimage")]
    public bool enableAfterimage = true;
    [Range(1, 5)] public int afterimageCount = 3;
    public float afterimageFade = 0.3f;

    private TrailRenderer trail;
    private Material trailMaterial;
    private int currentCombo = 1;
    private Color baseEmissionColor;

    void Start()
    {
        trail = GetComponent<TrailRenderer>();
        if (trail == null) return;

        // Cache material
        trailMaterial = trail.material;
        if (trailMaterial.HasProperty("_EmissionColor"))
        {
            baseEmissionColor = trailMaterial.GetColor("_EmissionColor");
        }

        // Initialize default gradients
        InitializeDefaultGradients();

        // Apply base settings
        ApplyComboSettings(1);
    }

    void InitializeDefaultGradients()
    {
        // Combo 1: Yellow -> Orange (Standard)
        if (combo1Gradient == null || combo1Gradient.colorKeys.Length == 0)
        {
            combo1Gradient = new Gradient();
            combo1Gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.9f, 0.3f), 0f),
                    new GradientColorKey(new Color(1f, 0.5f, 0f), 0.5f),
                    new GradientColorKey(new Color(0.8f, 0.2f, 0f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
        }

        // Combo 2: Orange -> Red (Heating Up)
        if (combo2Gradient == null || combo2Gradient.colorKeys.Length == 0)
        {
            combo2Gradient = new Gradient();
            combo2Gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.6f, 0f), 0f),
                    new GradientColorKey(new Color(1f, 0.2f, 0f), 0.5f),
                    new GradientColorKey(new Color(0.6f, 0f, 0f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.9f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
        }

        // Combo 3: White -> Blue-White (Max Power / "Holy" Effect)
        if (combo3Gradient == null || combo3Gradient.colorKeys.Length == 0)
        {
            combo3Gradient = new Gradient();
            combo3Gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.8f, 0.9f, 1f), 0.3f),
                    new GradientColorKey(new Color(0.4f, 0.6f, 1f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
        }
    }

    /// <summary>
    /// Call this when combo changes (from SimpleCombo.cs)
    /// </summary>
    public void SetCombo(int combo)
    {
        currentCombo = Mathf.Clamp(combo, 1, 3);
        ApplyComboSettings(currentCombo);
    }

    void ApplyComboSettings(int combo)
    {
        if (trail == null) return;

        // Select gradient
        Gradient selectedGradient = combo switch
        {
            1 => combo1Gradient,
            2 => combo2Gradient,
            3 => combo3Gradient,
            _ => combo1Gradient
        };
        trail.colorGradient = selectedGradient;

        // Scale width based on combo
        float widthMult = Mathf.Lerp(1f, maxWidthMultiplier, (combo - 1) / 2f);
        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0f, baseWidth * widthMult);
        widthCurve.AddKey(0.15f, baseWidth * widthMult * 0.7f);
        widthCurve.AddKey(1f, 0f);
        trail.widthCurve = widthCurve;

        // Boost emission on higher combos
        if (trailMaterial != null && trailMaterial.HasProperty("_EmissionColor"))
        {
            float emissionMult = Mathf.Lerp(1f, maxEmissionMultiplier, (combo - 1) / 2f);
            trailMaterial.SetColor("_EmissionColor", baseEmissionColor * emissionMult);
        }

        // Adjust trail time (longer at higher combos for more dramatic effect)
        trail.time = trailTime * (1f + (combo - 1) * 0.2f);
    }

    /// <summary>
    /// Trigger a burst effect (for heavy impacts)
    /// </summary>
    public void TriggerBurst()
    {
        if (trail == null) return;

        // Temporarily boost width
        StartCoroutine(BurstCoroutine());
    }

    private System.Collections.IEnumerator BurstCoroutine()
    {
        float originalTime = trail.time;
        trail.time = trailTime * 2f;

        yield return new WaitForSeconds(0.2f);

        trail.time = originalTime;
    }
}
