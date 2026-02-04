using UnityEngine;
using UnityEngine.Events;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// POV Twist Manager - THE FULL NARRATIVE
///
/// FLOW:
/// 1. Player enters cave after killing 3 waves
/// 2. Screen flash, slow-mo, lights dim
/// 3. POV SWITCH: Player now controls a Beast (the victim)
/// 4. The Hero (now AI) walks into the cave
/// 5. Hero charges at you and kills you
/// 6. "TO BE CONTINUED"
/// </summary>
public class POVTwistManager : MonoBehaviour
{
    [Header("Timing")]
    public float slowMoDuration = 2.0f;
    [Range(0.01f, 0.5f)] public float slowMoScale = 0.1f;
    public float transitionDuration = 1.5f;
    public float waitBeforeHeroEnters = 3.0f;
    public float heroApproachTime = 4.0f;

    [Header("Phase 1: Hero Mode")]
    public Color heroAmbient = Color.gray;

    [Header("Phase 2: Reality Mode")]
    public Color realityAmbient = new Color(0.1f, 0.1f, 0.2f);
    public Color warmFirelight = new Color(1f, 0.6f, 0.3f);

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip heartbeatClip;
    public AudioClip impactClip;
    public AudioClip swordUnsheatheClip;

    [Header("References")]
    public PixelationEffect pixelEffect;
    public Light directionalLight;
    public Transform caveInterior; // Where the beast spawns/waits

    [Header("Events")]
    public UnityEvent OnTwistStart;
    public UnityEvent OnTwistComplete;

    private bool isTwisting = false;
    private GameObject playerHero;
    private GameObject playerBeast;
    private bool showToBeContinued = false;

    void Start()
    {
        // Find the hero
        playerHero = GameObject.FindGameObjectWithTag("Player");
    }

    public void TriggerTwist()
    {
        if (isTwisting) return;
        StartCoroutine(FullTwistSequence());
    }

    private IEnumerator FullTwistSequence()
    {
        isTwisting = true;
        OnTwistStart?.Invoke();
        Debug.Log("[POVTwist] === SCENE B: THE TWIST BEGINS ===");

        // ========================================
        // PHASE 1: THE IMPACT (Flash, Slow-Mo)
        // ========================================

        // Screen Flash
        flashAlpha = 1f;

        // Slow Motion
        Time.timeScale = slowMoScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // Heartbeat
        if (sfxSource && heartbeatClip)
        {
            sfxSource.clip = heartbeatClip;
            sfxSource.loop = true;
            sfxSource.Play();
        }

        yield return new WaitForSecondsRealtime(slowMoDuration);

        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;
        if (sfxSource) sfxSource.Stop();

        // ========================================
        // PHASE 2: LIGHTS OUT, POV SWITCH
        // ========================================

        // Kill the sun
        if (directionalLight) directionalLight.enabled = false;
        RenderSettings.ambientLight = Color.black;
        RenderSettings.ambientIntensity = 0f;

        // Fade to black
        fadeAlpha = 0f;
        while (fadeAlpha < 1f)
        {
            fadeAlpha += Time.deltaTime * 2f;
            yield return null;
        }
        fadeAlpha = 1f;

        yield return new WaitForSeconds(1.0f);

        // ========================================
        // PHASE 3: YOU ARE NOW THE MONSTER
        // ========================================

        // Disable player controls
        if (playerHero != null)
        {
            var heroMove = playerHero.GetComponent<HeroMovement>();
            if (heroMove) heroMove.enabled = false;
            var heroCombo = playerHero.GetComponent<SimpleCombo>();
            if (heroCombo) heroCombo.enabled = false;
        }

        // Find or create a Beast to become the player
        GameObject[] beasts = GameObject.FindGameObjectsWithTag("Enemy");
        if (beasts.Length > 0)
        {
            playerBeast = beasts[0];
        }
        else
        {
            // Spawn a new beast for the player to "be"
            GameObject beastPrefab = Resources.Load<GameObject>("Beast");
            if (beastPrefab != null)
            {
                Vector3 spawnPos = caveInterior != null ? caveInterior.position : Vector3.zero;
                playerBeast = Instantiate(beastPrefab, spawnPos, Quaternion.identity);
            }
        }

        // Disable beast AI - player will "control" it (but can only wait)
        if (playerBeast != null)
        {
            var beastAI = playerBeast.GetComponent<BeastAI>();
            if (beastAI) beastAI.enabled = false;
            var beastNav = playerBeast.GetComponent<NavMeshAgent>();
            if (beastNav) beastNav.enabled = false;

            // Make it look innocent (warm colors)
            var renderer = playerBeast.GetComponentInChildren<Renderer>();
            if (renderer)
            {
                renderer.material.color = new Color(0.6f, 0.8f, 1.0f); // Soft blue - innocent
            }
        }

        // Switch camera to look at the beast (the new "player")
        Camera mainCam = Camera.main;
        if (mainCam != null && playerBeast != null)
        {
            // Position camera to show the cozy den
            Vector3 beastPos = playerBeast.transform.position;
            mainCam.transform.position = beastPos + new Vector3(0, 5, -6);
            mainCam.transform.LookAt(beastPos + Vector3.up);
        }

        // Warm firelight (cozy den feeling)
        RenderSettings.ambientLight = warmFirelight * 0.3f;

        // Find or create a warm light source
        Light caveLight = null;
        var spawner = FindObjectOfType<Spawner>();
        if (spawner != null && spawner.caveLight != null)
        {
            caveLight = spawner.caveLight;
            caveLight.color = warmFirelight;
            caveLight.intensity = 50f;
        }

        // Fade back in
        while (fadeAlpha > 0f)
        {
            fadeAlpha -= Time.deltaTime * 0.5f; // Slow fade in
            yield return null;
        }
        fadeAlpha = 0f;

        Debug.Log("[POVTwist] You are now the Beast. Waiting in the den...");

        // ========================================
        // PHASE 4: THE WAIT (Tension)
        // ========================================

        // Display "Wait for Mama..." or just silence
        showWaitingText = true;
        yield return new WaitForSeconds(waitBeforeHeroEnters);
        showWaitingText = false;

        // ========================================
        // PHASE 5: THE HERO ENTERS
        // ========================================

        Debug.Log("[POVTwist] The Hero enters the den...");

        // Sword unsheathe sound
        if (sfxSource && swordUnsheatheClip)
        {
            sfxSource.PlayOneShot(swordUnsheatheClip);
        }

        // Move the Hero towards the beast
        if (playerHero != null && playerBeast != null)
        {
            // Re-enable hero visuals, make them menacing
            var heroRenderer = playerHero.GetComponentInChildren<Renderer>();
            if (heroRenderer)
            {
                heroRenderer.material.color = Color.red; // Blood-stained
            }

            // Enable sword
            var combo = playerHero.GetComponent<SimpleCombo>();
            if (combo != null && combo.bladeMesh != null)
            {
                combo.bladeMesh.enabled = true;
            }

            // Walk towards beast
            Vector3 startPos = playerHero.transform.position;
            Vector3 endPos = playerBeast.transform.position;
            float elapsed = 0f;

            // Make hero face the beast
            Vector3 lookDir = (endPos - startPos).normalized;
            lookDir.y = 0;
            playerHero.transform.rotation = Quaternion.LookRotation(lookDir);

            while (elapsed < heroApproachTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / heroApproachTime;
                t = Mathf.SmoothStep(0, 1, t);

                // Move hero
                CharacterController heroCC = playerHero.GetComponent<CharacterController>();
                if (heroCC != null)
                {
                    heroCC.enabled = false;
                    playerHero.transform.position = Vector3.Lerp(startPos, endPos, t);
                }
                else
                {
                    playerHero.transform.position = Vector3.Lerp(startPos, endPos, t);
                }

                yield return null;
            }
        }

        // ========================================
        // PHASE 6: THE KILL
        // ========================================

        Debug.Log("[POVTwist] The Hero strikes...");

        // Screen shake
        var shake = FindObjectOfType<CameraShake>();
        if (shake) shake.Shake(0.5f, 0.8f);

        // Flash and cut to black
        flashAlpha = 1f;
        yield return new WaitForSeconds(0.3f);

        // Destroy the beast (you died)
        if (playerBeast != null)
        {
            Destroy(playerBeast);
        }

        // Full black
        fadeAlpha = 1f;

        yield return new WaitForSeconds(1.5f);

        // ========================================
        // PHASE 7: TO BE CONTINUED
        // ========================================

        Debug.Log("[POVTwist] === TO BE CONTINUED ===");
        showToBeContinued = true;

        isTwisting = false;
        OnTwistComplete?.Invoke();
    }

    // ========================================
    // GUI RENDERING
    // ========================================

    private float flashAlpha = 0f;
    private float fadeAlpha = 0f;
    private bool showWaitingText = false;
    private GUIStyle textStyle;

    void OnGUI()
    {
        // Initialize style
        if (textStyle == null)
        {
            textStyle = new GUIStyle();
            textStyle.fontSize = 48;
            textStyle.fontStyle = FontStyle.Italic;
            textStyle.alignment = TextAnchor.MiddleCenter;
            textStyle.normal.textColor = Color.white;
        }

        // Screen flash (white)
        if (flashAlpha > 0)
        {
            GUI.color = new Color(1, 1, 1, flashAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            flashAlpha -= Time.unscaledDeltaTime * 3f;
        }

        // Fade to black
        if (fadeAlpha > 0)
        {
            GUI.color = new Color(0, 0, 0, fadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        }

        // "Waiting..." text
        if (showWaitingText)
        {
            GUI.color = new Color(1, 1, 1, 0.5f);
            Rect waitRect = new Rect(0, Screen.height - 100, Screen.width, 50);
            textStyle.fontSize = 24;
            GUI.Label(waitRect, "...", textStyle);
            textStyle.fontSize = 48;
        }

        // "TO BE CONTINUED" text
        if (showToBeContinued)
        {
            // Dark background
            GUI.color = new Color(0, 0, 0, 0.85f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

            // Shadow
            GUI.color = new Color(0, 0, 0, 1f);
            Rect shadowRect = new Rect(4, Screen.height / 2 - 46, Screen.width, 100);
            GUI.Label(shadowRect, "To Be Continued...", textStyle);

            // Main text
            GUI.color = Color.white;
            Rect textRect = new Rect(0, Screen.height / 2 - 50, Screen.width, 100);
            GUI.Label(textRect, "To Be Continued...", textStyle);

            // Restart hint
            textStyle.fontSize = 20;
            GUI.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            Rect hintRect = new Rect(0, Screen.height / 2 + 50, Screen.width, 50);
            GUI.Label(hintRect, "[ Click to Restart ]", textStyle);
            textStyle.fontSize = 48;

            // Handle restart
            if (Input.GetMouseButtonDown(0))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                );
            }
        }
    }
}
