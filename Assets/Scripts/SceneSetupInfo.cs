using UnityEngine;
using UnityEngine.UI;

public class SceneSetupInfo : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CheckScene()
    {
        // 1. Check for Spawner
        var spawner = Object.FindObjectOfType<Spawner>();
        if (spawner == null)
        {
            Debug.LogError("MISSING: You need a GameObject with the 'Spawner' script!");
            GameObject obj = new GameObject("GameManager_Spawner");
            spawner = obj.AddComponent<Spawner>();
            Debug.Log("Created 'GameManager_Spawner' for you. BUT you must drag the Beast prefab into its 'Beast Prefab' slot in Inspector!");
        }
        else
        {
            if (spawner.beastPrefab == null)
            {
                Debug.LogError("SETUP REQUIRED: Click on '" + spawner.name + "' and drag the 'Beast' prefab into the 'Beast Prefab' slot!");
            }
        }

        // 2. Check for Cave
        var cave = Object.FindObjectOfType<CaveEndingTrigger>();
        if (cave == null)
        {
            Debug.LogWarning("Creating default Cave Trigger...");
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.name = "CaveTrigger";
            c.transform.position = new Vector3(8, 0, 8); // Put it somewhere
            c.transform.localScale = Vector3.one * 3;
            c.GetComponent<Collider>().isTrigger = true;
            c.GetComponent<MeshRenderer>().enabled = false; // Hide it
            c.AddComponent<CaveEndingTrigger>();

            // The SetupCave script will handle the light
            // But we can ensure SetupCave runs
            GameObject setupObj = new GameObject("CaveSetupHelper");
            setupObj.AddComponent<SetupCave>();
        }

        // 3. UI Check
        // Spawner creates UI on Start(), so if Spawner exists, UI should exist.
        // We can force a test canvas if needed.
    }
}
