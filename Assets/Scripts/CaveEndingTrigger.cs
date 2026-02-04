using UnityEngine;

public class CaveEndingTrigger : MonoBehaviour
{
    private bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

        if (other.CompareTag("Player"))
        {
            // Only allow ending if Spawner says waves are done
            if (Spawner.Instance != null && !Spawner.Instance.WavesAreComplete)
            {
                Debug.Log("[CaveEndingTrigger] Waves not complete yet!");
                return;
            }

            triggered = true;
            Debug.Log("[CaveEndingTrigger] All waves complete! Triggering THE TWIST...");

            // Disable the collider so player can walk INTO the cave
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // Also disable any child colliders (walls)
            foreach (var childCol in GetComponentsInChildren<Collider>())
            {
                childCol.enabled = false;
            }

            // Trigger the full POV Twist sequence
            // This handles: Flash -> Slow-mo -> You become the monster -> Hero kills you -> To Be Continued
            POVTwistManager twist = FindObjectOfType<POVTwistManager>();
            if (twist != null)
            {
                twist.TriggerTwist();
            }
            else
            {
                Debug.LogError("[CaveEndingTrigger] No POVTwistManager found! Add one to the scene.");
            }

            // Stop the background music loop (optional - POVTwistManager handles atmosphere)
            if (BackgroundMusicLoader.Instance != null)
            {
                // Just stop looping, let POVTwistManager control the ending
                // BackgroundMusicLoader.Instance.TriggerEndingSequence(); // REMOVED - POVTwist handles this now
            }
        }
    }
}
