using UnityEngine;
using System.Collections;

/// <summary>
/// Simple Combo + Combat Juice
/// Adds: Lunge on attack, Hit Stop on impact, Sword collider auto-sizing
/// </summary>
public class SimpleCombo : MonoBehaviour
{
    private static SimpleCombo _instance;
    public static SimpleCombo Instance { get { return _instance; } }

    public Transform swordPivot;
    public MeshRenderer bladeMesh;
    [Header("Combat Feel")]
    public float maxComboDelay = 0.5f;
    public float lungeForce = 5f; // Forward burst when attacking
    public float hitStopDuration = 0.05f; // Freeze frame on impact

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip comboSound;

    private int comboIndex = 0;
    private float lastClickTime;
    public bool isAttacking = false;
    public bool isHarmful = false;

    [HideInInspector] public bool justAttacked = false;
    [HideInInspector] public int currentAttackAnim = 0;

    private Rigidbody rb;
    private BoxCollider bladeCollider;
    private CharacterController controller;
    private Vector3 lungeVelocity;

    void Awake()
    {
        _instance = this;
        rb = GetComponent<Rigidbody>();
        controller = GetComponent<CharacterController>();
    }

    void Start()
    {
        // Hide sword at start
        if (bladeMesh != null) bladeMesh.enabled = false;

        // Hide Trail at start
        var trail = GetComponentInChildren<TrailRenderer>();
        if (trail != null) trail.emitting = false;

        // Auto-setup Audio
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

        if (comboSound == null)
            comboSound = Resources.Load<AudioClip>("Music/SwordWhoosh");

        // Ensure collider exists on the blade mesh object
        if (bladeMesh != null)
        {
            bladeCollider = bladeMesh.GetComponent<BoxCollider>();
            if (bladeCollider == null)
            {
                bladeCollider = bladeMesh.gameObject.AddComponent<BoxCollider>();
                bladeCollider.isTrigger = true;
                // Make it big and generous
                bladeCollider.size = new Vector3(0.5f, 2.5f, 0.5f);
                Debug.Log("Created auto-collider for sword.");
            }
            bladeMesh.gameObject.name = "Blade"; // Ensure name matches BeastHealth check
        }
    }

    void Update()
    {
        if (Time.time - lastClickTime > maxComboDelay) comboIndex = 0;

        // Prevent attacking while paused (e.g. twist)
        if (Input.GetMouseButtonDown(0) && !isAttacking && Time.timeScale > 0.1f)
        {
            // FACE THE MOUSE - using same logic as AimReticle
            Camera cam = Camera.main;
            Vector3 mousePos = Input.mousePosition;

            // Handle PixelationEffect RenderTexture
            if (cam != null && cam.targetTexture != null)
            {
                float normalizedX = mousePos.x / Screen.width;
                float normalizedY = mousePos.y / Screen.height;
                mousePos.x = normalizedX * cam.targetTexture.width;
                mousePos.y = normalizedY * cam.targetTexture.height;
            }

            Plane playerPlane = new Plane(Vector3.up, transform.position);
            Ray ray = cam.ScreenPointToRay(mousePos);
            if (playerPlane.Raycast(ray, out float hitDist))
            {
                Vector3 targetPoint = ray.GetPoint(hitDist);
                // Keep y same as transform to avoid tilting
                targetPoint.y = transform.position.y;
                Quaternion targetRotation = Quaternion.LookRotation(targetPoint - transform.position);
                transform.rotation = targetRotation;
            }

            // LUNGE FORWARD (Juice) - Use CharacterController if no Rigidbody
            if (controller != null)
            {
                // Apply instant lunge via CharacterController
                lungeVelocity = transform.forward * lungeForce;
            }
            else if (rb != null)
            {
                rb.AddForce(transform.forward * lungeForce, ForceMode.Impulse);
            }

            lastClickTime = Time.time;
            comboIndex++;
            if (comboIndex > 3) comboIndex = 1;

            justAttacked = true;
            currentAttackAnim = comboIndex;

            StartCoroutine(SwingSword(comboIndex));
        }
    }

    void LateUpdate()
    {
        justAttacked = false;

        // Apply lunge velocity (decays over time)
        if (controller != null && lungeVelocity.magnitude > 0.1f)
        {
            controller.Move(lungeVelocity * Time.deltaTime);
            lungeVelocity = Vector3.Lerp(lungeVelocity, Vector3.zero, Time.deltaTime * 10f);
        }
    }

    IEnumerator SwingSword(int step)
    {
        isAttacking = true;
        if (bladeMesh != null) bladeMesh.enabled = true;

        // Start Trail
        var trail = GetComponentInChildren<TrailRenderer>();
        if (trail != null) trail.emitting = true;
        // Notify enhancer if it exists
        var flameEnhancer = GetComponentInChildren<FlameTrailEnhancer>();
        if(flameEnhancer) flameEnhancer.SetCombo(step);

        // Define positions
        Quaternion idleRot = Quaternion.identity;
        Quaternion windupRot = (step == 3) ? Quaternion.Euler(-100, 0, 0) : Quaternion.Euler(0, (step == 1 ? -45 : 45), 0);
        Quaternion strikeRot = (step == 3) ? Quaternion.Euler(90, 0, 0) : Quaternion.Euler(0, (step == 1 ? 90 : -90), 0);

        // PHASE 1: WIND-UP
        float t = 0;
        float windupSpeed = (step == 3 ? 2f : 5f);
        while (t < 1)
        {
            t += Time.deltaTime * windupSpeed;
            swordPivot.localRotation = Quaternion.Slerp(idleRot, windupRot, t);
            yield return null;
        }

        // PHASE 2: THE STRIKE
        t = 0;
        isHarmful = true;

        // Sound
        if (audioSource != null && comboSound != null)
        {
            audioSource.pitch = (step == 3) ? 0.7f : Random.Range(1.1f, 1.3f);
            audioSource.PlayOneShot(comboSound);
        }

        // Swing
        while (t < 1)
        {
            t += Time.deltaTime * 15f;
            swordPivot.localRotation = Quaternion.Slerp(windupRot, strikeRot, t);
            yield return null;
        }
        isHarmful = false;

        // PHASE 3: RECOVERY
        if (step == 3)
        {
            if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.15f, 0.4f);
            // Trigger burst effect on trail
            if(flameEnhancer) flameEnhancer.TriggerBurst();
        }

        yield return new WaitForSeconds(0.1f);

        if (trail != null) trail.emitting = false;
        if (bladeMesh != null) bladeMesh.enabled = false;
        swordPivot.localRotation = idleRot;
        isAttacking = false;
    }

    /// <summary>
    /// Call this when hitting an enemy to freeze frame briefly
    /// </summary>
    public void TriggerHitStop()
    {
        StartCoroutine(HitStopRoutine());
    }

    IEnumerator HitStopRoutine()
    {
        if (Time.timeScale < 0.1f) yield break; // Don't interfere with Twist

        float oldScale = Time.timeScale;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = oldScale;
    }
}