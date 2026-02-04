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

            // Trigger the BackgroundMusicLoader ending sequence
            // This handles: Dramatic music (2:25), Lights out, Camera pan, Player walk into cave
            // After that completes, it automatically triggers POVTwistManager for Part 2
            if (BackgroundMusicLoader.Instance != null)
            {
                BackgroundMusicLoader.Instance.TriggerEndingSequence();
            }
            else
            {
                // Fallback: trigger POVTwist directly if no music loader
                POVTwistManager twist = FindObjectOfType<POVTwistManager>();
                if (twist != null) twist.TriggerTwist();
            }
        }
    }
}
