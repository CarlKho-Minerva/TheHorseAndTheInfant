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
    private GameObject[] caveWalls; // Dynamically created cave walls

    void Start()
    {
        // Find the hero
        playerHero = GameObject.FindGameObjectWithTag("Player");
    }

    /// <summary>
    /// Creates a dark cave atmosphere for Scene B - NO WALLS, just darkness + scaled ground
    /// </summary>
    void CreateCaveEnvironment()
    {
        // Change skybox to solid black
        RenderSettings.skybox = null;
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = Color.black;

        // Dim ambient light significantly
        RenderSettings.ambientLight = new Color(0.05f, 0.03f, 0.02f); // Very dark
        RenderSettings.ambientIntensity = 0.2f;

        // Find and scale up the ground to make it feel like a vast dark floor
        GameObject ground = GameObject.Find("Ground");
        if (ground != null)
        {
            // Scale ground horizontally (X and Z) but NOT vertically (Y)
            Vector3 currentScale = ground.transform.localScale;
            ground.transform.localScale = new Vector3(currentScale.x * 3f, currentScale.y, currentScale.z * 3f);

            // Darken the ground material
            var groundRenderer = ground.GetComponent<Renderer>();
            if (groundRenderer != null)
            {
                groundRenderer.material.color = new Color(0.1f, 0.08f, 0.06f); // Dark brown cave floor
            }
        }

        // Destroy the original Cave object walls if they exist
        // GameObject cave = GameObject.Find("Cave");
        // if (cave != null)
        // {
            // cave.SetActive(false); // USER REQUEST: Don't hide the cave!
        // }

        Debug.Log("[POVTwist] Cave environment created - darkened scene, scaled ground.");
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
        // PHASE 1: IMMEDIATE FADE TO BLACK (hide current scene)
        // ========================================

        // Instant fade to black - hide everything while we set up Scene B
        fadeAlpha = 1f;

        // Kill the sun immediately (during black screen)
        if (directionalLight) directionalLight.enabled = false;
        RenderSettings.ambientLight = Color.black;
        RenderSettings.ambientIntensity = 0f;

        // Disable player controls
        if (playerHero != null)
        {
            var heroMove = playerHero.GetComponent<HeroMovement>();
            if (heroMove) heroMove.enabled = false;
            var heroCombo = playerHero.GetComponent<SimpleCombo>();
            if (heroCombo) heroCombo.enabled = false;

            // Hide the hero temporarily
            playerHero.SetActive(false);
        }

        yield return new WaitForSeconds(0.5f); // Brief pause on black

        // ========================================
        // PHASE 2: SET UP SCENE B (while screen is black)
        // ========================================

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

        // Disable beast AI - player will "control" it
        if (playerBeast != null)
        {
            var beastAI = playerBeast.GetComponent<BeastAI>();
            if (beastAI) beastAI.enabled = false;
            var beastNav = playerBeast.GetComponent<NavMeshAgent>();
            if (beastNav) beastNav.enabled = false;

            // Make it GREEN - it's a slime!
            var renderers = playerBeast.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.material.color = new Color(0.3f, 0.8f, 0.3f); // Green slime
            }

            // FIX: Make it FLATTENED (Horizontal Slime Shape)
            // Flatten Y, Expand X/Z
            playerBeast.transform.localScale = new Vector3(1.4f, 0.6f, 1.4f);

            // FIX: Ensure beast is ABOVE ground (Y = 1.0 for proper visibility of the flattened shape)
            Vector3 beastPos = playerBeast.transform.position;
            beastPos.y = 1.0f;
            playerBeast.transform.position = beastPos;
        }

        // Create cave environment WHILE SCREEN IS BLACK
        CreateCaveEnvironment();

        // Switch camera to look at the beast (the new "player")
        Camera mainCam = Camera.main;
        if (mainCam != null && playerBeast != null)
        {
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
            caveLight.gameObject.SetActive(true);
            caveLight.enabled = true;
            caveLight.color = warmFirelight;
            caveLight.intensity = 50f;
        }

        yield return new WaitForSeconds(0.3f); // Let everything settle

        // ========================================
        // PHASE 3: FADE IN TO SCENE B (everything is ready)
        // ========================================

        Debug.Log("[POVTwist] You are now the Beast. Waiting in the den...");

        // Slow fade in
        while (fadeAlpha > 0f)
        {
            fadeAlpha -= Time.deltaTime * 0.8f;
            yield return null;
        }
        fadeAlpha = 0f;

        // ========================================
        // PHASE 4: PLAYER CAN MOVE AS THE SLIME
        // ========================================

        showWaitingText = true;
        float moveTime = waitBeforeHeroEnters;
        float moveElapsed = 0f;
        Camera followCam = Camera.main;

        while (moveElapsed < moveTime)
        {
            moveElapsed += Time.deltaTime;

            // Allow player to move the beast around
            if (playerBeast != null)
            {
                float h = Input.GetAxisRaw("Horizontal");
                float v = Input.GetAxisRaw("Vertical");
                Vector3 moveDir = new Vector3(h, 0, v).normalized;

                // Simple movement (no physics)
                playerBeast.transform.position += moveDir * 3f * Time.deltaTime;

                // Keep beast ABOVE ground
                Vector3 pos = playerBeast.transform.position;
                pos.y = 1.0f;
                playerBeast.transform.position = pos;

                // Update camera to follow
                if (followCam != null)
                {
                    Vector3 beastPos = playerBeast.transform.position;
                    Vector3 targetCamPos = beastPos + new Vector3(0, 5, -6);
                    followCam.transform.position = Vector3.Lerp(followCam.transform.position, targetCamPos, Time.deltaTime * 3f);
                    followCam.transform.LookAt(beastPos + Vector3.up);
                }
            }

            yield return null;
        }

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

        // Move the Hero towards the beast but STOP before reaching
        if (playerHero != null && playerBeast != null)
        {
            // Show hero again
            playerHero.SetActive(true);

            // Place hero INSIDE the cave (spawn from the cave entrance area)
            // Use the Cave object if available for proper positioning
            GameObject cave = GameObject.Find("Cave");
            Vector3 beastPos = playerBeast.transform.position;
            Vector3 heroStartPos;

            if (cave != null)
            {
                // Spawn at cave entrance, facing outward toward the slime
                heroStartPos = cave.transform.position + new Vector3(0, 1.0f, 0);
            }
            else
            {
                // Fallback: spawn behind/above the beast
                heroStartPos = beastPos + new Vector3(0, 0, 12f);
                heroStartPos.y = 1.0f;
            }
            playerHero.transform.position = heroStartPos;

            // Get all hero renderers for fade-in effect
            var heroRenderers = playerHero.GetComponentsInChildren<Renderer>();

            // Start fully transparent
            foreach (var r in heroRenderers)
            {
                if (r.material.HasProperty("_Color"))
                {
                    Color c = r.material.color;
                    c.a = 0f;
                    r.material.color = c;
                }
            }

            // FADE IN the hero over 1 second
            float fadeInTime = 1.0f;
            float fadeElapsed = 0f;
            while (fadeElapsed < fadeInTime)
            {
                fadeElapsed += Time.deltaTime;
                float alpha = fadeElapsed / fadeInTime;

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

            // Ensure fully visible and make them menacing RED
            foreach (var r in heroRenderers)
            {
                if (r.material.HasProperty("_Color"))
                {
                    Color c = Color.red; // Blood-stained
                    c.a = 1f;
                    r.material.color = c;
                }
            }

            // Enable sword
            var combo = playerHero.GetComponent<SimpleCombo>();
            if (combo != null && combo.bladeMesh != null)
            {
                combo.bladeMesh.enabled = true;
            }

            // Calculate stop position (stop 2 units in front of beast)
            float stopDistance = 2.0f;
            Vector3 dirToBeast = (beastPos - heroStartPos).normalized;
            Vector3 stopPos = beastPos - dirToBeast * stopDistance;
            stopPos.y = 1.0f; // Keep hero at correct height

            // Make hero face the beast
            Vector3 lookDir = dirToBeast;
            lookDir.y = 0;
            playerHero.transform.rotation = Quaternion.LookRotation(lookDir);

            // Walk towards beast - STOP at distance
            float elapsed = 0f;
            Vector3 startPos = heroStartPos;

            while (elapsed < heroApproachTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / heroApproachTime;
                t = Mathf.SmoothStep(0, 1, t);

                // Move hero to stop position (not to beast position!)
                CharacterController heroCC = playerHero.GetComponent<CharacterController>();
                if (heroCC != null) heroCC.enabled = false;
                playerHero.transform.position = Vector3.Lerp(startPos, stopPos, t);

                // Keep camera following
                if (followCam != null)
                {
                    Vector3 camTarget = playerBeast.transform.position;
                    Vector3 targetCamPos = camTarget + new Vector3(0, 5, -6);
                    followCam.transform.position = Vector3.Lerp(followCam.transform.position, targetCamPos, Time.deltaTime * 2f);
                    followCam.transform.LookAt(camTarget + Vector3.up);
                }

                yield return null;
            }

            // Brief pause - hero is now standing in front of you
            yield return new WaitForSeconds(0.5f);

            // ========================================
            // PHASE 6: THE STRIKE - SLOW MO HERE!
            // ========================================

            Debug.Log("[POVTwist] The Hero strikes... SLOW MOTION!");

            // SLOW MOTION ON THE STRIKE
            Time.timeScale = slowMoScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            // Heartbeat during slow-mo
            if (sfxSource && heartbeatClip)
            {
                sfxSource.clip = heartbeatClip;
                sfxSource.loop = true;
                sfxSource.Play();
            }

            // Screen shake
            var shake = FindObjectOfType<CameraShake>();
            if (shake) shake.Shake(0.5f, 0.8f);

            // Flash
            flashAlpha = 1f;

            // MAKE THE HERO SWING THE SWORD during slow-mo!
            var heroCombo = playerHero.GetComponent<SimpleCombo>();
            if (heroCombo != null && heroCombo.swordPivot != null)
            {
                // Show the sword
                if (heroCombo.bladeMesh != null) heroCombo.bladeMesh.enabled = true;

                // Animate sword swing in slow motion (using real time)
                float swingSequenceDuration = slowMoDuration * 1.5f; // Extend duration for windup

                // Windup Phase
                float windupTime = 0.5f;
                Quaternion startRot = Quaternion.Euler(0, 80, 0);  // Windup position
                Quaternion endRot = Quaternion.Euler(0, -80, 0);   // Strike position

                // Hold Windup
                heroCombo.swordPivot.localRotation = startRot;
                yield return new WaitForSecondsRealtime(windupTime);

                // Strike Phase
                float swingElapsed = 0f;
                float swingSpeed = 0.4f; // Slower swing visual

                // Start Fade DURING the swing
                StartCoroutine(FadeOutDuringStrike(swingSpeed * 0.8f)); // Fade completely before swing ends

                while (swingElapsed < swingSpeed)
                {
                    swingElapsed += Time.unscaledDeltaTime;
                    float t = swingElapsed / swingSpeed;
                    t = Mathf.SmoothStep(0, 1, t); // Smooth easing
                    heroCombo.swordPivot.localRotation = Quaternion.Slerp(startRot, endRot, t);
                    yield return null;
                }
            }
            else
            {
                // Fallback: just wait
                yield return new WaitForSecondsRealtime(slowMoDuration);
            }

            // Wait a moment on black screen
            yield return new WaitForSecondsRealtime(1.0f);

            // Restore time
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;
            if (sfxSource) sfxSource.Stop();
            if (sfxSource) sfxSource.Stop();

            // Destroy the beast (you died - off screen)
            if (playerBeast != null)
            {
                Destroy(playerBeast);
            }

            yield return new WaitForSeconds(1.5f);
        }

        // ========================================
        // PHASE 7: TO BE CONTINUED
        // ========================================

        Debug.Log("[POVTwist] === TO BE CONTINUED ===");
        showToBeContinued = true;

        isTwisting = false;
        OnTwistComplete?.Invoke();
    }

    private IEnumerator FadeOutDuringStrike(float duration)
    {
        float elapsed = 0f;
        while(elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeAlpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        fadeAlpha = 1f;
    }

    // ========================================
    // GUI RENDERING
    // ========================================

    private float flashAlpha = 0f;
    private float fadeAlpha = 0f;
    private bool showWaitingText = false;
    private GUIStyle textStyle;
    private GUIStyle smallTextStyle;

    void OnGUI()
    {
        // Initialize styles (use built-in font to avoid dynamic font issues)
        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.label);
            textStyle.fontSize = 48;
            textStyle.fontStyle = FontStyle.Bold;
            textStyle.alignment = TextAnchor.MiddleCenter;
            textStyle.normal.textColor = Color.white;
        }

        if (smallTextStyle == null)
        {
            smallTextStyle = new GUIStyle(GUI.skin.label);
            smallTextStyle.fontSize = 20;
            smallTextStyle.alignment = TextAnchor.MiddleCenter;
            smallTextStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
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
            GUI.Label(waitRect, "...", smallTextStyle);
        }

        // GUILT TRIP ENDING
        if (showToBeContinued)
        {
            // Dark background
            GUI.color = new Color(0, 0, 0, 0.95f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

            // Main guilt message
            GUI.color = new Color(0.9f, 0.3f, 0.3f); // Blood red
            Rect mainRect = new Rect(0, Screen.height / 2 - 80, Screen.width, 60);
            GUI.Label(mainRect, "That was a baby.", textStyle);

            // Second line
            GUI.color = new Color(0.8f, 0.8f, 0.8f);
            Rect line2Rect = new Rect(0, Screen.height / 2 - 20, Screen.width, 50);
            GUI.Label(line2Rect, "How satisfying was that kill?", smallTextStyle);

            // Meta confession
            GUI.color = new Color(0.5f, 0.5f, 0.5f);
            Rect metaRect = new Rect(0, Screen.height / 2 + 40, Screen.width, 50);
            GUI.Label(metaRect, "(Carl didn't have time to flesh out the story.", smallTextStyle);

            Rect meta2Rect = new Rect(0, Screen.height / 2 + 70, Screen.width, 50);
            GUI.Label(meta2Rect, "Tell Prof. Watson you felt guilty.)", smallTextStyle);

            // Restart hint
            GUI.color = new Color(0.4f, 0.4f, 0.4f, 1f);
            Rect hintRect = new Rect(0, Screen.height / 2 + 130, Screen.width, 50);
            GUI.Label(hintRect, "[ Click to Restart ]", smallTextStyle);

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
