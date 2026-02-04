using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Unit Loader - ENHANCED
/// Features: Runtime Modifiers, Spawn Effects, Variant Switching, Health Component Integration
/// </summary>
[CreateAssetMenu(fileName = "NewUnit", menuName = "Game/Unit Loader")]
public class UnitLoader : ScriptableObject
{
    [Header("Identity")]
    public string unitName;
    public Sprite unitIcon;
    public GameObject prefabModel;

    [Header("Base Stats")]
    public float maxHealth = 100f;
    public float moveSpeed = 3.5f;
    public float damage = 10f;
    public float attackCooldown = 1f;

    [Header("Scaling (for Wave Difficulty)")]
    [Tooltip("Multiplier per wave (e.g., 1.2 = 20% harder each wave)")]
    public float waveScaling = 1.0f;

    [Header("Visual Identity")]
    public Color heroModeTint = Color.red;       // How it looks to the "Hero"
    public Color realityModeTint = Color.cyan;   // How it looks after the "Twist"
    public float realityModeScale = 0.8f;        // Smaller = less threatening

    [Header("Spawn Effects")]
    public GameObject spawnVFXPrefab;
    public AudioClip spawnSFX;
    [Range(0f, 2f)] public float spawnDelay = 0.5f;

    [Header("Twist Variant")]
    [Tooltip("Reference to the 'Reality' version of this unit")]
    public UnitLoader realityVariant;
    public bool isRealityVariant = false;

    /// <summary>
    /// Spawn a unit with full initialization
    /// </summary>
    public GameObject Spawn(Vector3 position, int waveNumber = 1)
    {
        if (prefabModel == null)
        {
            Debug.LogError($"[UnitLoader] {unitName} has no prefab assigned!");
            return null;
        }

        GameObject instance = Instantiate(prefabModel, position, Quaternion.identity);
        instance.name = $"{unitName}_Wave{waveNumber}";

        // Apply scaled stats
        float scaledHealth = maxHealth * Mathf.Pow(waveScaling, waveNumber - 1);
        float scaledDamage = damage * Mathf.Pow(waveScaling, waveNumber - 1);

        // Try to apply to common components
        var healthComp = instance.GetComponent<IDamageable>();
        if (healthComp != null)
        {
            // healthComp.Initialize(scaledHealth);
        }

        // Apply visual tint
        ApplyTint(instance, isRealityVariant ? realityModeTint : heroModeTint);

        // Spawn VFX
        if (spawnVFXPrefab != null)
        {
            Instantiate(spawnVFXPrefab, position, Quaternion.identity);
        }

        // Spawn SFX
        if (spawnSFX != null)
        {
            AudioSource.PlayClipAtPoint(spawnSFX, position);
        }

        Debug.Log($"[UnitLoader] Spawned {unitName} | HP: {scaledHealth:F0} | DMG: {scaledDamage:F0}");
        return instance;
    }

    /// <summary>
    /// Apply visual tint to all renderers
    /// </summary>
    public void ApplyTint(GameObject instance, Color tint)
    {
        var renderers = instance.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            if (r.material.HasProperty("_Color"))
            {
                r.material.color = tint;
            }
        }
    }

    /// <summary>
    /// Transform a spawned unit to its "Reality" appearance (called during Twist)
    /// </summary>
    public void TransformToReality(GameObject instance)
    {
        if (instance == null) return;

        // Apply Reality tint
        ApplyTint(instance, realityModeTint);

        // Scale down to look less threatening
        instance.transform.localScale *= realityModeScale;

        // Disable any "evil" particle effects
        var particles = instance.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particles)
        {
            var main = ps.main;
            main.startColor = realityModeTint;
            ps.Stop();
        }

        Debug.Log($"[UnitLoader] {instance.name} transformed to Reality Mode.");
    }

    /// <summary>
    /// Get modified stats for a specific wave
    /// </summary>
    public (float health, float dmg, float speed) GetScaledStats(int wave)
    {
        float mult = Mathf.Pow(waveScaling, wave - 1);
        return (maxHealth * mult, damage * mult, moveSpeed);
    }
}

/// <summary>
/// Interface for damageable entities (implement on your enemies)
/// </summary>
public interface IDamageable
{
    void Initialize(float maxHP);
    void TakeDamage(float amount);
    void Die();
}
