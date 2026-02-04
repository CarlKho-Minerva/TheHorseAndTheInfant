using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Beast AI - Handles movement/chasing only.
/// Damage is handled by BeastHealth.cs
/// </summary>
public class BeastAI : MonoBehaviour {
    public Transform target;
    NavMeshAgent agent;

    [Header("Chase Settings")]
    [Tooltip("Distance at which beast stops chasing (prevents overlap)")]
    public float stoppingDistance = 1.5f;
    [Tooltip("Distance at which beast starts chasing again")]
    public float resumeChaseDistance = 2.5f;

    private bool isWaiting = false;

    void Start() {
        agent = GetComponent<NavMeshAgent>();

        // Find player automatically (with null check)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
        }
        else
        {
            Debug.LogWarning("[BeastAI] No Player found with tag 'Player'!");
        }

        // Set NavMeshAgent stopping distance
        if (agent != null)
        {
            agent.stoppingDistance = stoppingDistance;
        }

        // NOTE: Hitbox enlargement is handled by BeastHealth.cs
    }

    void Update() {
        // Chase player if we have a target and agent
        if (target == null || agent == null || !agent.enabled) return;

        float distanceToPlayer = Vector3.Distance(transform.position, target.position);

        // Stop when too close to prevent overlap
        if (distanceToPlayer <= stoppingDistance)
        {
            isWaiting = true;
            agent.isStopped = true;
        }
        // Resume chasing when player moves away
        else if (distanceToPlayer >= resumeChaseDistance)
        {
            isWaiting = false;
            agent.isStopped = false;
        }

        // Only update destination if not waiting
        if (!isWaiting)
        {
            agent.SetDestination(target.position);
        }

        // Always face the player when close
        if (isWaiting)
        {
            Vector3 lookDir = (target.position - transform.position).normalized;
            lookDir.y = 0; // Keep upright
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);
            }
        }
    }

    // NOTE: OnTriggerEnter for damage is handled by BeastHealth.cs
}