using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class BackgroundMusicLoader : MonoBehaviour
{
    public static BackgroundMusicLoader Instance { get; private set; }

    private AudioSource source;
    private bool isEnding = false;

    // Music Timings
    private const float START_TIME = 4.0f;
    private const float LOOP_END_TIME = 143.0f; // 2:23
    private const float ENDING_START_TIME = 145.0f; // 2:25
    // private const float ENDING_STOP_TIME = 148.0f; // 2:28 (Removed per request)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        if (Instance != null) return;
        GameObject bgmObject = new GameObject("BackgroundMusicLoader");
        Instance = bgmObject.AddComponent<BackgroundMusicLoader>();
        DontDestroyOnLoad(bgmObject);
    }

    void Start()
    {
        AudioClip clip = Resources.Load<AudioClip>("Music/BackgroundMusic");

        if (clip != null)
        {
            source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = false; // We handle looping manually
            source.playOnAwake = false;
            source.volume = 0.5f;
            source.time = START_TIME;
            source.Play();
            Debug.Log($"Background music loaded and started at {START_TIME}s");
        }
        else
        {
            Debug.LogError("BackgroundMusicLoader: Could not load 'Music/BackgroundMusic'.");
        }
    }

    void Update()
    {
        if (source == null || !source.isPlaying) return;

        if (!isEnding)
        {
            // Loop Logic: 0:04 -> 2:23
            if (source.time >= LOOP_END_TIME)
            {
                source.time = START_TIME;
            }
        }
        else
        {
            // Ending Logic: Just keep playing until end of track or loop manually if needed.
            // User requested: "rest of the music still plays"
        }
    }

    public void TriggerEndingSequence()
    {
        if (isEnding) return;
        StartCoroutine(EndingRoutine());
    }

    private IEnumerator EndingRoutine()
    {
        isEnding = true;

        // 1. Disable Player Control but keep Component access
        GameObject hero = GameObject.FindGameObjectWithTag("Player");
        CharacterController heroController = null;

        if (hero != null)
        {
            var movement = hero.GetComponent<HeroMovement>();
            if (movement != null) movement.enabled = false;
            var combo = hero.GetComponent<SimpleCombo>();
            if (combo != null) combo.enabled = false;

            // We want to move him manually, so ensure we have the controller
            heroController = hero.GetComponent<CharacterController>();

            // Don't turn on isKinematic if we use CharacterController, it handles its own physics usually.
            // But if there is a Rigidbody, we might want it kinematic so gravity doesn't mess up our manual move?
            // Usually CharacterController doesn't use RB.
        }

        // Disable Beasts
        var beasts = FindObjectsOfType<BeastAI>();
        foreach(var b in beasts) b.enabled = false;

        // LIGHTS OUT EFFECT!
        // 1. Find Directional Light and disable it
        Light[] lights = FindObjectsOfType<Light>();
        foreach(var l in lights)
        {
            // Don't turn off the Cave Light! (Assuming it has "Cave" in name or is the one we want)
            // Or simplest: Only turn off Directional lights
            if (l.type == LightType.Directional)
            {
                l.enabled = false;
            }
        }
        // 2. Set Ambient to black
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.black;
        // Note: If using Skybox, might need to dim it:
        RenderSettings.ambientIntensity = 0f;
        RenderSettings.reflectionIntensity = 0f;

        // 3. Play Music 2:25 -> Onward
        Debug.Log("Playing Ending Music Segment...");
        source.time = ENDING_START_TIME;

        // 2. Reposition Camera (Cinematic Zoom)
        // AND Move Player into the Cave (Simulate walking forward)
        Camera mainCam = Camera.main;
        if (mainCam != null && hero != null)
        {
            Vector3 startPos = mainCam.transform.position;
            Quaternion startRot = mainCam.transform.rotation;

            // Zoom in slightly from above/behind
            Vector3 camEndPos = hero.transform.position + new Vector3(0, 4, -4);
            Quaternion camEndRot = Quaternion.LookRotation(hero.transform.position - camEndPos);

            // Calculate walk direction (use current forward)
            Vector3 walkDir = hero.transform.forward;

            // Disable CharacterController to allow phasing through walls
            if (heroController != null) heroController.enabled = false;

            // Get hero renderers to fade them out
            Renderer[] heroRenderers = hero.GetComponentsInChildren<Renderer>();

            float t = 0;
            float duration = 4.0f; // 4 seconds of walking/zooming (longer walk)

            while(t < 1)
            {
                float dt = Time.unscaledDeltaTime; // Use unscaled incase you paused physics? (We didn't here)
                t += dt / duration;

                // Camera Move
                mainCam.transform.position = Vector3.Lerp(startPos, camEndPos, t);
                mainCam.transform.rotation = Quaternion.Slerp(startRot, camEndRot, t);

                // Player Walk - Manual Transform Move (Phasing)
                if (hero != null)
                {
                    hero.transform.position += walkDir * 2f * dt; // Walk speed 2
                }

                // FADE HERO OUT as they walk into cave (disappear into darkness)
                // Start fading at t=0.3, fully invisible by t=0.8
                float fadeStart = 0.3f;
                float fadeEnd = 0.8f;
                float fadeT = Mathf.InverseLerp(fadeStart, fadeEnd, t);
                float alpha = 1f - fadeT;

                foreach (var r in heroRenderers)
                {
                    if (r.material.HasProperty("_Color"))
                    {
                        Color c = r.material.color;
                        c.a = alpha;
                        r.material.color = c;
                    }
                }

                yield return null;
            }

            // Ensure hero is fully invisible at end
            foreach (var r in heroRenderers)
            {
                if (r.material.HasProperty("_Color"))
                {
                    Color c = r.material.color;
                    c.a = 0f;
                    r.material.color = c;
                }
            }
        }

        // 4. TRIGGER THE POV TWIST (Part 2: Become the slime, get slain)
        POVTwistManager twist = FindObjectOfType<POVTwistManager>();
        if (twist != null)
        {
            Debug.Log("[BackgroundMusicLoader] Handing off to POVTwistManager for Part 2...");
            twist.TriggerTwist();
            // POVTwistManager handles the rest (slime POV, hero strike, To Be Continued)
            yield break; // Exit this coroutine - POVTwist takes over
        }
        else
        {
            // Fallback if no POVTwistManager - show ending here
            Debug.LogWarning("[BackgroundMusicLoader] No POVTwistManager found, showing ending directly.");
            CreateEndingUI();

            // 5. Wait for Restart
            yield return new WaitForSeconds(1f);
            while (!Input.GetMouseButtonDown(0))
            {
                yield return null;
            }

            Destroy(gameObject);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    // Use OnGUI for guaranteed text rendering
    private bool showEndingText = false;
    private float endingFadeAlpha = 0f;
    private GUIStyle endingStyle;

    private void CreateEndingUI()
    {
        Debug.Log("=== ENDING UI TRIGGERED ===");
        showEndingText = true;
        StartCoroutine(FadeInEnding());
    }

    IEnumerator FadeInEnding()
    {
        float t = 0;
        while(t < 1)
        {
            t += Time.unscaledDeltaTime * 0.5f;
            endingFadeAlpha = t;
            yield return null;
        }
        endingFadeAlpha = 1f;
    }

    void OnGUI()
    {
        if (!showEndingText) return;

        // Draw black background
        GUI.color = new Color(0, 0, 0, endingFadeAlpha * 0.85f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

        // Setup style for text
        if (endingStyle == null)
        {
            endingStyle = new GUIStyle(GUI.skin.label);
            endingStyle.fontSize = Mathf.Max(60, Screen.height / 12);
            endingStyle.alignment = TextAnchor.MiddleCenter;
            endingStyle.fontStyle = FontStyle.Bold;
        }

        // Draw text with shadow
        GUI.color = new Color(0, 0, 0, endingFadeAlpha);
        Rect shadowRect = new Rect(4, 4, Screen.width, Screen.height);
        GUI.Label(shadowRect, "TO BE CONTINUED...", endingStyle);

        // Main text
        GUI.color = new Color(1, 1, 1, endingFadeAlpha);
        Rect textRect = new Rect(0, 0, Screen.width, Screen.height);
        GUI.Label(textRect, "TO BE CONTINUED...", endingStyle);

        // Click to restart hint
        if (endingFadeAlpha > 0.5f)
        {
            GUIStyle smallStyle = new GUIStyle(GUI.skin.label);
            smallStyle.fontSize = Mathf.Max(20, Screen.height / 40);
            smallStyle.alignment = TextAnchor.MiddleCenter;
            GUI.color = new Color(0.7f, 0.7f, 0.7f, endingFadeAlpha * 0.8f);
            Rect hintRect = new Rect(0, Screen.height * 0.65f, Screen.width, 50);
            GUI.Label(hintRect, "[ Click to Restart ]", smallStyle);
        }
    }
}
