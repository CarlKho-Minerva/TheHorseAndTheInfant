using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// Beast Health System - Handles damage, knockback, and death
/// Features: Health Pool, Hit Stagger, Knockback Physics, Boss Mode, Death VFX
/// </summary>
[RequireComponent(typeof(Collider))]
public class BeastHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 3f;
    public float currentHealth;
    public bool isBoss = false;
    [Tooltip("Boss has higher HP multiplier")]
    public float bossHealthMultiplier = 5f;

    [Header("Knockback")]
    public float knockbackForce = 8f;
    public float knockbackDuration = 0.3f;
    public bool useRigidbodyKnockback = true;

    [Header("Hit Feedback")]
    public Color hitFlashColor = Color.white;
    public float hitFlashDuration = 0.1f;
    public float hitStaggerDuration = 0.2f;
    public bool showDamageNumbers = true;

    [Header("Death")]
    public GameObject deathVFXPrefab;
    public AudioClip deathSFX;
    public AudioClip hitSFX;
    public float deathDelay = 0.5f;

    [Header("References")]
    public AudioSource audioSource;

    private Renderer[] renderers;
    private Color[] originalColors;
    private NavMeshAgent navAgent;
    private Rigidbody rb;
    private bool isDead = false;
    // private bool isStaggered = false; // Removed as it was unused and causing warnings

    void Start()
    {
        // Initialize health
        currentHealth = isBoss ? maxHealth * bossHealthMultiplier : maxHealth;

        // Cache renderers for flash effect
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_Color"))
                originalColors[i] = renderers[i].material.color;
        }

        // Cache components
        navAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        // Enlarge hitbox for satisfying hits
        EnlargeHitbox();
    }

    void EnlargeHitbox()
    {
        var col = GetComponent<Collider>();
        if (col == null) return;

        // Make hitbox 2x larger for easier hits
        if (col is CapsuleCollider cap)
        {
            cap.radius *= 2.0f;
            cap.height *= 1.5f;
        }
        else if (col is SphereCollider sph) sph.radius *= 2.0f;
        else if (col is BoxCollider box) box.size *= 2.0f;
    }

    public void Initialize(float maxHP)
    {
        maxHealth = maxHP;
        currentHealth = maxHP;
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, Vector3.zero);
    }

    public void TakeDamage(float amount, Vector3 hitDirection)
    {
        if (isDead) return;

        currentHealth -= amount;

        // Hit feedback
        StartCoroutine(HitFlash());
        if (hitSFX && audioSource) audioSource.PlayOneShot(hitSFX);

        // Squash & Stretch effect (Juice)
        var squash = GetComponent<SquashStretch>();
        if (squash != null) squash.TriggerSquash();

        // Check if this is the fatal blow for the final enemy (Matrix Mode)
        bool isFinalKill = (currentHealth <= 0 && Spawner.Instance != null && Spawner.Instance.IsLastEnemyOfFinalWave());

        // HIT STOP (Juice) - ONLY if NOT the final kill (conflicts with Matrix slow mo)
        if (SimpleCombo.Instance != null && amount > 0 && !isFinalKill)
        {
            SimpleCombo.Instance.TriggerHitStop();
        }

        // Knockback
        if (knockbackForce > 0 && hitDirection != Vector3.zero)
        {
            StartCoroutine(ApplyKnockback(hitDirection));
        }

        // Damage number (optional)
        if (showDamageNumbers)
        {
            // You could instantiate a floating text here
            Debug.Log($"[BeastHealth] {gameObject.name} took {amount} damage! HP: {currentHealth}/{maxHealth}");
        }

        // Check death
        if (currentHealth <= 0)
        {
            Debug.Log($"[BeastHealth] {gameObject.name} DYING! Checking for final kill...");

            // CHECK FOR FINAL KILL SLOW MO - BEFORE Die() is called
            if (Spawner.Instance != null)
            {
                Debug.Log("[BeastHealth] Spawner exists, checking IsLastEnemyOfFinalWave...");
                if (Spawner.Instance.IsLastEnemyOfFinalWave())
                {
                    Debug.Log("[BeastHealth] FINAL KILL! Requesting Matrix Slow-Mo from Spawner!");
                    // Trigger slow mo via Spawner (so it persists even if this object dies)
                    Spawner.Instance.TriggerMatrixSlowMo();
                }
            }
            else
            {
                Debug.LogWarning("[BeastHealth] Spawner.Instance is NULL!");
            }

            Die();
        }
    }

    /* Moved to Spawner.cs to ensure persistence
    IEnumerator MatrixSlowMo()
    {
        // Instant freeze/slow
        Time.timeScale = 0.1f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // Wait for a moment (realtime)
        yield return new WaitForSecondsRealtime(2.0f);

        // Resume
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;
    }
    */

    IEnumerator HitFlash()
    {
        // Flash white
        foreach (var r in renderers)
        {
            if (r.material.HasProperty("_Color"))
                r.material.color = hitFlashColor;
        }

        yield return new WaitForSeconds(hitFlashDuration);

        // Restore original colors
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = originalColors[i];
        }
    }

    IEnumerator ApplyKnockback(Vector3 direction)
    {
        // isStaggered = true;

        // Disable NavMeshAgent during knockback
        if (navAgent) navAgent.enabled = false;

        if (useRigidbodyKnockback && rb != null)
        {
            // Use rigidbody physics
            rb.isKinematic = false;
            rb.AddForce(direction.normalized * knockbackForce, ForceMode.Impulse);
        }
        else
        {
            // Manual position lerp knockback
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + direction.normalized * (knockbackForce * 0.5f);
            float elapsed = 0f;

            while (elapsed < knockbackDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / knockbackDuration;
                t = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic
                transform.position = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }
        }

        yield return new WaitForSeconds(hitStaggerDuration);

        // Re-enable NavMeshAgent
        if (navAgent)
        {
            navAgent.enabled = true;
            navAgent.Warp(transform.position); // Sync position
        }

        if (rb) rb.isKinematic = true;
        // isStaggered = false;
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // Notify spawner
        if (Spawner.Instance != null) Spawner.Instance.OnBeastKilled();

        // Death VFX
        if (deathVFXPrefab)
        {
            Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
        }

        // Death SFX
        if (deathSFX)
        {
            AudioSource.PlayClipAtPoint(deathSFX, transform.position);
        }

        // Delay destruction for death animation
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        // Disable movement
        if (navAgent) navAgent.enabled = false;

        // Shrink/fade out
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;

        while (elapsed < deathDelay)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / deathDelay;

            // Shrink
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            // Fade (if materials support it)
            foreach (var r in renderers)
            {
                if (r.material.HasProperty("_Color"))
                {
                    Color c = r.material.color;
                    c.a = 1f - t;
                    r.material.color = c;
                }
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    // Called by SimpleCombo's blade trigger
    void OnTriggerEnter(Collider other)
    {
        // Check if it's the blade (or any child of the hero with the sword)
        bool isBlade = other.name == "Blade" || other.name.Contains("Sword") || other.name.Contains("Blade");

        // Also check parent
        if (!isBlade && other.transform.parent != null)
        {
            isBlade = other.transform.parent.name.Contains("Sword") || other.transform.parent.name.Contains("Pivot");
        }

        if (isBlade && !isDead)
        {
            GameObject hero = GameObject.FindGameObjectWithTag("Player");
            SimpleCombo combo = hero?.GetComponent<SimpleCombo>();

            // More generous check - if sword is visible, it's harmful
            if (combo != null && (combo.isHarmful || combo.isAttacking || combo.justAttacked))
            {
                // Calculate knockback direction (away from player)
                Vector3 knockDir = (transform.position - hero.transform.position).normalized;

                // Damage based on combo step (combo 3 = heavy hit)
                float damage = combo.currentAttackAnim == 3 ? 2f : 1f;

                Debug.Log($"[BeastHealth] HIT! Damage: {damage}, Combo: {combo.currentAttackAnim}");
                TakeDamage(damage, knockDir);
            }
        }
    }
}
