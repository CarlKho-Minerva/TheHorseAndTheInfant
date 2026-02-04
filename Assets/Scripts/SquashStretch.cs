using UnityEngine;

/// <summary>
/// Squash & Stretch - Makes objects feel alive and reactive
/// Attach to any object that should respond to impacts
/// </summary>
public class SquashStretch : MonoBehaviour
{
    [Header("Settings")]
    public float squashAmount = 0.7f;
    public float stretchAmount = 1.2f;
    public float returnSpeed = 8f;

    [Header("Idle Breathing")]
    public bool enableBreathing = true;
    public float breathingSpeed = 2f;
    public float breathingAmount = 0.05f;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private bool isSquashing = false;

    void Start()
    {
        originalScale = transform.localScale;
        // Ensure original scale is positive to avoid BoxCollider warnings
        originalScale.x = Mathf.Abs(originalScale.x);
        originalScale.y = Mathf.Abs(originalScale.y);
        originalScale.z = Mathf.Abs(originalScale.z);
        targetScale = originalScale;
        transform.localScale = originalScale; // Apply the fix
    }

    void Update()
    {
        // Smooth return to target scale
        Vector3 nextScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * returnSpeed);

        // Prevent negative or zero scale which breaks BoxCollider
        nextScale.x = Mathf.Max(nextScale.x, 0.01f);
        nextScale.y = Mathf.Max(nextScale.y, 0.01f);
        nextScale.z = Mathf.Max(nextScale.z, 0.01f);

        transform.localScale = nextScale;

        // Idle breathing animation
        if (enableBreathing && !isSquashing)
        {
            float breathe = 1f + Mathf.Sin(Time.time * breathingSpeed) * breathingAmount;
            Vector3 breatheScale = originalScale * breathe;

             // Ensure breathing target is also safe
            breatheScale.x = Mathf.Max(breatheScale.x, 0.01f);
            breatheScale.y = Mathf.Max(breatheScale.y, 0.01f);
            breatheScale.z = Mathf.Max(breatheScale.z, 0.01f);

            targetScale = breatheScale;
        }
    }

    /// <summary>
    /// Call this when the beast takes a hit
    /// </summary>
    public void TriggerSquash()
    {
        StartCoroutine(SquashRoutine());
    }

    /// <summary>
    /// Call this when the beast jumps or lunges
    /// </summary>
    public void TriggerStretch()
    {
        StartCoroutine(StretchRoutine());
    }

    private System.Collections.IEnumerator SquashRoutine()
    {
        isSquashing = true;

        // Squash (flatten)
        targetScale = new Vector3(
            originalScale.x * stretchAmount,
            originalScale.y * squashAmount,
            originalScale.z * stretchAmount
        );

        yield return new WaitForSeconds(0.1f);

        // Return to normal
        targetScale = originalScale;
        isSquashing = false;
    }

    private System.Collections.IEnumerator StretchRoutine()
    {
        isSquashing = true;

        // Stretch (elongate)
        targetScale = new Vector3(
            originalScale.x * squashAmount,
            originalScale.y * stretchAmount,
            originalScale.z * squashAmount
        );

        yield return new WaitForSeconds(0.1f);

        // Return to normal
        targetScale = originalScale;
        isSquashing = false;
    }
}
