using UnityEngine;

/// <summary>
/// Force Field - Personal Space Protection
/// Pushes enemies away if they get too close (prevents clipping)
/// NOTE: Disabled by default - enable only if enemies clip INTO the player
/// </summary>
public class PlayerForceField : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Enable this only if enemies clip into the player")]
    public bool enableForceField = false; // DISABLED BY DEFAULT
    [Tooltip("Radius of personal space - keep small!")]
    public float radius = 0.8f; // Much smaller - only prevent overlap
    [Tooltip("Strength of the repulsion force")]
    public float pushForce = 2.0f; // Weaker push

    void Update()
    {
        // Only run if enabled
        if (!enableForceField) return;
        // Find all enemies in radius
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                // Push them away
                PushEnemy(hit.gameObject);
            }
        }
    }

    void PushEnemy(GameObject enemy)
    {
        Vector3 direction = (enemy.transform.position - transform.position).normalized;

        // 1. Try NavMeshAgent (Velocity override)
        var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.isActiveAndEnabled)
        {
            // Nudge velocity away
            agent.velocity += direction * pushForce * Time.deltaTime;
        }

        // 2. Try Rigidbody (Physics push)
        var rb = enemy.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.AddForce(direction * pushForce * 5f * Time.deltaTime, ForceMode.VelocityChange);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
