using UnityEngine;

/// <summary>
/// Hero Movement - ENHANCED
/// Features: WASD Movement, Boundary Clamp, Instant Turn
/// Snaps to floor dynamically to prevent floating/sinking
/// </summary>
public class HeroMovement : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 8f;

    [Header("Map Boundaries")]
    [Tooltip("If assigned, bounds are calculated automatically from this object's MeshRenderer")]
    public Transform groundObject;

    [Tooltip("Prevents falling off the map (used if Ground Object is null)")]
    public float minX = -12f;
    public float maxX = 12f;
    public float minZ = -12f;
    public float maxZ = 12f;

    [Header("Ground Snapping")]
    [Tooltip("Layer(s) to consider as ground - should NOT include Enemy layer!")]
    public LayerMask groundLayer = 1; // Default layer only
    public float heightOffset = 1.0f;

    [Header("Edge Blocking")]
    [Tooltip("Stop player before the actual edge")]
    public float edgeBuffer = 0.5f; // Small buffer

    private CharacterController controller;
    private Vector3 moveDir;

    void Start() {
        controller = GetComponent<CharacterController>();

        // Auto-detect bounds from ground object if assigned
        if (groundObject != null)
        {
            Renderer r = groundObject.GetComponent<Renderer>();
            if (r != null)
            {
                Bounds b = r.bounds;
                // Calculate bounds with buffer
                minX = b.min.x + edgeBuffer;
                maxX = b.max.x - edgeBuffer;
                minZ = b.min.z + edgeBuffer;
                maxZ = b.max.z - edgeBuffer;
                Debug.Log($"[HeroMovement] Auto-configured bounds from {groundObject.name}: X[{minX}, {maxX}] Z[{minZ}, {maxZ}]");
            }
            else
            {
                // Fallback to manual scale/pos calculation if no renderer (e.g. just collider)
                // Assuming standard cube scaling
                Vector3 center = groundObject.position;
                Vector3 size = groundObject.lossyScale; // Approximate size
                minX = center.x - size.x/2 + edgeBuffer;
                maxX = center.x + size.x/2 - edgeBuffer;
                minZ = center.z - size.z/2 + edgeBuffer;
                maxZ = center.z + size.z/2 - edgeBuffer;
            }
        }
        else
        {
            // Try to find object named "Ground" automatically
            GameObject g = GameObject.Find("Ground");
            if (g != null)
            {
                groundObject = g.transform;
                Start(); // Retry setup
                return;
            }
        }

        SnapToGround();
    }

    float GetGroundHeight()
    {
        // Raycast down to find ONLY ground (not enemies or player)
        Vector3 origin = transform.position + Vector3.up * 1.0f;

        // Use the groundLayer mask - make sure it doesn't include Enemy layer!
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 5.0f, groundLayer, QueryTriggerInteraction.Ignore))
        {
            // Extra safety: skip if we hit ourselves or an enemy
            if (hit.collider.gameObject == gameObject) return GetFallbackGroundHeight();
            if (hit.collider.CompareTag("Enemy")) return GetFallbackGroundHeight();

            return hit.point.y + heightOffset;
        }

        return GetFallbackGroundHeight();
    }

    // Fallback: return a safe ground height when raycast fails or hits enemy
    float GetFallbackGroundHeight()
    {
        // If we're way too high, pull us back down
        if (transform.position.y > heightOffset + 0.5f)
        {
            return heightOffset; // Default ground level
        }
        return transform.position.y;
    }

    void SnapToGround()
    {
        float y = GetGroundHeight();
        if (controller != null) controller.enabled = false;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
        if (controller != null) controller.enabled = true;
    }

    void Update() {
        // Get Input
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        moveDir = new Vector3(x, 0, z).normalized;

        // Move
        if (controller != null)
        {
            // Check if move would push us past boundary BEFORE moving
            Vector3 nextPos = transform.position + moveDir * speed * Time.deltaTime;

            // Block movement at edges (invisible wall effect)
            if (nextPos.x < minX || nextPos.x > maxX)
            {
                moveDir.x = 0; // Block X movement
            }
            if (nextPos.z < minZ || nextPos.z > maxZ)
            {
                moveDir.z = 0; // Block Z movement
            }

            controller.Move(moveDir * speed * Time.deltaTime);

            // Hard clamp as safety net
            Vector3 pos = transform.position;
            float clampedX = Mathf.Clamp(pos.x, minX, maxX);
            float clampedZ = Mathf.Clamp(pos.z, minZ, maxZ);

            // Snap Y to ground
            float targetY = GetGroundHeight();

            if (pos.x != clampedX || pos.z != clampedZ || Mathf.Abs(pos.y - targetY) > 0.05f)
            {
                controller.enabled = false;
                transform.position = new Vector3(clampedX, targetY, clampedZ);
                controller.enabled = true;
            }
        }

        // Face Movement Direction
        if (moveDir != Vector3.zero) {
            transform.forward = moveDir;
        }
    }
}