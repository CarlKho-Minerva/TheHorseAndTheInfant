using UnityEngine;
using System.Collections;

public class Spawner : MonoBehaviour
{
    public static Spawner Instance;

    [Header("Setup")]
    public GameObject beastPrefab;

    [Header("Cave Setup")]
    public GameObject caveObject;
    public Light caveLight;

    [Header("Cave Light Settings")]
    [Tooltip("Dim flickering during gameplay")]
    public float dimIntensity = 15f;
    [Tooltip("Bright pulsing after waves complete")]
    public float brightIntensity = 100f;
    public float flickerSpeed = 8f;

    private int currentWave = 0;
    private int beastsRemainingInWave = 0;
    private int[] waves = new int[] { 1, 2, 3 }; // Wave structure
    private bool wavesCompleted = false;
    public bool WavesAreComplete => wavesCompleted; // Public accessor for CaveTrigger

    private Coroutine lightCoroutine;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(StartupRoutine());
    }

    IEnumerator StartupRoutine()
    {
        yield return null;

        // Auto-find cave components
        if (caveObject == null) caveObject = GameObject.Find("CaveTrigger");
        if (caveObject == null) caveObject = FindObjectOfType<CaveEndingTrigger>()?.gameObject;

        if (caveLight == null && caveObject != null)
        {
            caveLight = caveObject.GetComponentInChildren<Light>();
        }

        // If no light exists, create one
        if (caveLight == null && caveObject != null)
        {
            GameObject lightObj = new GameObject("CaveGlow");
            lightObj.transform.SetParent(caveObject.transform);
            lightObj.transform.localPosition = Vector3.up * 2;
            caveLight = lightObj.AddComponent<Light>();
            caveLight.type = LightType.Point;
            caveLight.range = 50f;
            caveLight.color = new Color(1f, 0.4f, 0f); // Orange
            Debug.Log("Created orange light for cave.");
        }

        // Cave light stays OFF until all waves are complete
        if (caveLight != null)
        {
            caveLight.enabled = false;
            caveLight.intensity = 0f;
            Debug.Log($"Cave light initialized but OFF until waves complete");
        }
        else
        {
            Debug.LogError("Could not find or create cave light!");
        }

        StartCoroutine(StartWaveRoutine(0));
    }

    IEnumerator StartWaveRoutine(int waveIndex)
    {
        yield return new WaitForSeconds(2f); // Delay before wave starts

        // Safety check - can't spawn without a prefab
        if (beastPrefab == null)
        {
            Debug.LogError("[Spawner] beastPrefab is NULL! Drag the Beast prefab into the Inspector slot!");
            yield break;
        }

        currentWave = waveIndex;
        int count = waves[waveIndex];
        beastsRemainingInWave = count;

        Debug.Log($"Starting Wave {waveIndex + 1} with {count} beasts.");

        for (int i = 0; i < count; i++)
        {
            // Spawn from INSIDE the cave (behind the entrance)
            Vector3 origin;
            Vector3 spawnPos;

            if (caveObject != null)
            {
                origin = caveObject.transform.position;
                // Spawn BEHIND the cave entrance (inside the cave)
                // Use the cave's forward direction to determine "inside"
                Vector3 insideOffset = -caveObject.transform.forward * 3f; // 3 units inside
                Vector3 randomOffset = new Vector3(Random.Range(-1.5f, 1.5f), 0, Random.Range(-1f, 1f));
                spawnPos = origin + insideOffset + randomOffset;
            }
            else
            {
                // Fallback to spawner position
                origin = transform.position;
                spawnPos = origin + new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));
            }

            Instantiate(beastPrefab, spawnPos, Quaternion.identity);
            yield return new WaitForSeconds(1f); // Stagger spawns
        }
    }

    public void OnBeastKilled()
    {
        beastsRemainingInWave--;
        if (beastsRemainingInWave <= 0 && !wavesCompleted)
        {
            NextWave();
        }
    }

    void NextWave()
    {
        if (currentWave + 1 < waves.Length)
        {
            StartCoroutine(StartWaveRoutine(currentWave + 1));
        }
        else
        {
            Debug.Log("All waves completed! Activate Cave!");
            wavesCompleted = true;
            ActivateCave();
        }
    }

    void ActivateCave()
    {
        Debug.Log("ActivateCave called!");
        if (caveLight != null)
        {
            // Stop dim flickering, start bright pulsing
            if (lightCoroutine != null) StopCoroutine(lightCoroutine);

            caveLight.enabled = true;
            caveLight.intensity = brightIntensity;
            Debug.Log("Cave light enabled and pulsing BRIGHTLY!");
            lightCoroutine = StartCoroutine(PulseCaveLight());
        }
        else
        {
            Debug.LogError("Cave light is NULL when trying to activate!");
        }
    }

    /// <summary>
    /// Dim flickering during gameplay - subtle hint the cave is there
    /// </summary>
    IEnumerator FlickerCaveLight()
    {
        float t = 0;
        while (true)
        {
            t += Time.deltaTime * flickerSpeed;
            // Flicker between 50% and 100% of dim intensity
            float flicker = Mathf.PerlinNoise(t, 0f); // Smooth random
            caveLight.intensity = Mathf.Lerp(dimIntensity * 0.5f, dimIntensity, flicker);
            yield return null;
        }
    }

    /// <summary>
    /// Bright pulsing after waves complete - beckoning the player
    /// </summary>
    IEnumerator PulseCaveLight()
    {
        float t = 0;
        while(true)
        {
            t += Time.deltaTime;
            // Pulse intensity between 50% and 150% of bright intensity
            caveLight.intensity = Mathf.Lerp(brightIntensity * 0.5f, brightIntensity * 1.5f, (Mathf.Sin(t * 3f) + 1f) / 2f);
            yield return null;
        }
    }
}