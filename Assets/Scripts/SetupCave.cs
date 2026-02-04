using UnityEngine;

public class SetupCave : MonoBehaviour
{
    void Start()
    {
        // Script to ensure the Scene has the necessary Cave Setup for the Spawner logic
        // even if the user hasn't built it manually yet.

        var endingTrigger = FindObjectOfType<CaveEndingTrigger>();
        if (endingTrigger != null)
        {
            // Check if it has a light
            Light l = endingTrigger.GetComponentInChildren<Light>();
            if (l == null)
            {
                GameObject lightObj = new GameObject("CaveOrangeLight");
                lightObj.transform.SetParent(endingTrigger.transform);
                lightObj.transform.localPosition = Vector3.zero + Vector3.up * 2; // Lift it up
                l = lightObj.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = 15f;
                l.color = new Color(1f, 0.4f, 0f); // Deep Orange
                l.intensity = 0; // Spawner manages this
                l.shadows = LightShadows.Soft;
            }
        }
    }
}
