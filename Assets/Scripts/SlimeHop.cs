using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Procedural Slime Animation
/// Makes the object hop while moving. Works WITH NavMeshAgent.
/// DOES NOT affect root position - only animates a visual child or uses scale.
/// </summary>
public class SlimeHop : MonoBehaviour
{
    [Header("Hop Settings")]
    public float hopHeight = 0.3f;
    public float hopSpeed = 8f;
    public bool hopOnlyWhileMoving = true;

    [Header("Juice")]
    public SquashStretch squashStretch;
    public AudioClip landingSFX;

    [Header("References")]
    [Tooltip("Optional - if not set, will use scale-based animation")]
    public Transform visualChild;

    private Vector3 lastPos;
    private float timer;
    private bool isAirborne = false;
    private NavMeshAgent agent;
    private AudioSource audioSrc;

    void Start()
    {
        lastPos = transform.position;
        agent = GetComponent<NavMeshAgent>();
        audioSrc = GetComponent<AudioSource>();

        // Try to find visual child if not assigned
        if (visualChild == null)
        {
            visualChild = transform.Find("Visual");
        }

        // Auto-find squash stretch
        if (squashStretch == null)
        {
            squashStretch = GetComponent<SquashStretch>();
        }
    }

    void Update()
    {
        // 1. Calculate Velocity from NavMeshAgent or position delta
        float speed = 0f;
        if (agent != null && agent.enabled)
        {
            speed = agent.velocity.magnitude;
        }
        else
        {
            speed = Vector3.Distance(transform.position, lastPos) / Time.deltaTime;
        }
        lastPos = transform.position;

        // 2. Determine if we should hop
        bool shouldHop = !hopOnlyWhileMoving || speed > 0.5f;

        if (shouldHop)
        {
            timer += Time.deltaTime * hopSpeed;
        }
        else
        {
            // Smoothly stop hopping
            timer = Mathf.Lerp(timer, 0f, Time.deltaTime * 5f);
        }

        // 3. Calculate Hop Arc
        float hop = Mathf.Abs(Mathf.Sin(timer));
        float yOffset = hop * hopHeight;

        // 4. Apply hop to visual child ONLY (don't move root - NavMeshAgent controls that)
        if (visualChild != null)
        {
            visualChild.localPosition = new Vector3(0, yOffset, 0);
        }

        // 5. Landing Logic
        if (yOffset < 0.05f && isAirborne)
        {
            Land();
        }
        else if (yOffset > 0.15f)
        {
            isAirborne = true;
        }
    }

    void Land()
    {
        isAirborne = false;
        if (squashStretch != null) squashStretch.TriggerSquash();
        if (landingSFX != null && audioSrc != null)
            audioSrc.PlayOneShot(landingSFX);
    }
}
