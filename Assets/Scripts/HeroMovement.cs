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
    [Tooltip("Prevents falling off the map")]
    public float xBound = 14f;
    public float zBound = 14f;

    [Header("Ground Snapping")]
    [Tooltip("Layer(s) to consider as ground - should NOT include Enemy layer!")]
    public LayerMask groundLayer = 1; // Default layer only
    public float heightOffset = 1.0f;

    [Header("Edge Blocking")]
    [Tooltip("Stop player before the actual edge")]
    public float edgeBuffer = 1.0f; // Stop 1 unit before boundary

    private CharacterController controller;
    private Vector3 moveDir;
    private float actualXBound;
    private float actualZBound;

    void Start() {
        controller = GetComponent<CharacterController>();

        // Calculate actual bounds with buffer
        actualXBound = xBound - edgeBuffer;
        actualZBound = zBound - edgeBuffer;

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
            if (hit.collider.gameObject == gameObject) return transform.position.y;
            if (hit.collider.CompareTag("Enemy")) return transform.position.y;

            return hit.point.y + heightOffset;
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
            if (nextPos.x < -actualXBound || nextPos.x > actualXBound)
            {
                moveDir.x = 0; // Block X movement
            }
            if (nextPos.z < -actualZBound || nextPos.z > actualZBound)
            {
                moveDir.z = 0; // Block Z movement
            }

            controller.Move(moveDir * speed * Time.deltaTime);

            // Hard clamp as safety net
            Vector3 pos = transform.position;
            float clampedX = Mathf.Clamp(pos.x, -actualXBound, actualXBound);
            float clampedZ = Mathf.Clamp(pos.z, -actualZBound, actualZBound);

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