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
    /// Creates a dark cave enclosure for Scene B
    /// </summary>
    void CreateCaveEnvironment()
    {
        // Change skybox to solid black
        RenderSettings.skybox = null;
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = Color.black;

        // Create 4 walls around the player area
        caveWalls = new GameObject[4];
        Material darkMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        darkMat.color = new Color(0.1f, 0.08f, 0.06f); // Very dark brown

        Vector3 center = playerBeast != null ? playerBeast.transform.position : Vector3.zero;
        float wallSize = 20f;
        float wallHeight = 10f;

        // Front wall
        caveWalls[0] = GameObject.CreatePrimitive(PrimitiveType.Cube);
        caveWalls[0].transform.position = center + new Vector3(0, wallHeight / 2, wallSize / 2);
        caveWalls[0].transform.localScale = new Vector3(wallSize, wallHeight, 1);
        caveWalls[0].GetComponent<Renderer>().material = darkMat;
        caveWalls[0].name = "CaveWall_Front";
        // Ensure solid collider blocks enemies
        caveWalls[0].GetComponent<BoxCollider>().isTrigger = false;
        caveWalls[0].layer = LayerMask.NameToLayer("Default");

        // Back wall
        caveWalls[1] = GameObject.CreatePrimitive(PrimitiveType.Cube);
        caveWalls[1].transform.position = center + new Vector3(0, wallHeight / 2, -wallSize / 2);
        caveWalls[1].transform.localScale = new Vector3(wallSize, wallHeight, 1);
        caveWalls[1].GetComponent<Renderer>().material = darkMat;
        caveWalls[1].name = "CaveWall_Back";
        caveWalls[1].GetComponent<BoxCollider>().isTrigger = false;
        caveWalls[1].layer = LayerMask.NameToLayer("Default");

        // Left wall
        caveWalls[2] = GameObject.CreatePrimitive(PrimitiveType.Cube);
        caveWalls[2].transform.position = center + new Vector3(-wallSize / 2, wallHeight / 2, 0);
        caveWalls[2].transform.localScale = new Vector3(1, wallHeight, wallSize);
        caveWalls[2].GetComponent<Renderer>().material = darkMat;
        caveWalls[2].name = "CaveWall_Left";
        caveWalls[2].GetComponent<BoxCollider>().isTrigger = false;
        caveWalls[2].layer = LayerMask.NameToLayer("Default");

        // Right wall
        caveWalls[3] = GameObject.CreatePrimitive(PrimitiveType.Cube);
        caveWalls[3].transform.position = center + new Vector3(wallSize / 2, wallHeight / 2, 0);
        caveWalls[3].transform.localScale = new Vector3(1, wallHeight, wallSize);
        caveWalls[3].GetComponent<Renderer>().material = darkMat;
        caveWalls[3].name = "CaveWall_Right";
        caveWalls[3].GetComponent<BoxCollider>().isTrigger = false;
        caveWalls[3].layer = LayerMask.NameToLayer("Default");

        // Create a ceiling
        GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ceiling.transform.position = center + new Vector3(0, wallHeight, 0);
        ceiling.transform.localScale = new Vector3(wallSize, 1, wallSize);
        ceiling.GetComponent<Renderer>().material = darkMat;
        ceiling.name = "CaveCeiling";
        ceiling.GetComponent<BoxCollider>().isTrigger = false;
        ceiling.layer = LayerMask.NameToLayer("Default");

        // Bake the NavMesh obstacle so enemies can't walk through
        foreach (var wall in caveWalls)
        {
            if (wall != null)
            {
                var obstacle = wall.AddComponent<NavMeshObstacle>();
                obstacle.carving = true;
                obstacle.carveOnlyStationary = false;
            }
        }

        Debug.Log("[POVTwist] Cave environment created - you are now INSIDE the cave.");
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

            // Make it look innocent (warm colors)
            var renderer = playerBeast.GetComponentInChildren<Renderer>();
            if (renderer)
            {
                renderer.material.color = new Color(0.6f, 0.8f, 1.0f); // Soft blue - innocent
            }

            // FIX: Ensure beast is at ground level (Y = 0.5 for capsule center)
            Vector3 beastPos = playerBeast.transform.position;
            beastPos.y = 0.5f;
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

                // Keep beast at ground level
                Vector3 pos = playerBeast.transform.position;
                pos.y = 0.5f;
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

            // Place hero at cave entrance (outside the walls)
            Vector3 beastPos = playerBeast.transform.position;
            Vector3 heroStartPos = beastPos + new Vector3(0, 0, -15f); // Start far back
            heroStartPos.y = 0f;
            playerHero.transform.position = heroStartPos;

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

            // Calculate stop position (stop 2 units in front of beast)
            float stopDistance = 2.0f;
            Vector3 dirToBeast = (beastPos - heroStartPos).normalized;
            Vector3 stopPos = beastPos - dirToBeast * stopDistance;
            stopPos.y = 0f;

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

            // Hold slow-mo for dramatic effect
            yield return new WaitForSecondsRealtime(slowMoDuration);

            // FADE TO BLACK before the strike connects
            float fadeSpeed = 3f; // Fast fade
            while (fadeAlpha < 1f)
            {
                fadeAlpha += Time.unscaledDeltaTime * fadeSpeed;
                yield return null;
            }
            fadeAlpha = 1f;

            // Restore time
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;
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

    // ========================================
    // GUI RENDERING
    // ========================================

    private float flashAlpha = 0f;
    private float fadeAlpha = 0f;
    private bool showWaitingText = false;
    private GUIStyle textStyle;
    private Font pixelifyFont;

    void OnGUI()
    {
        // Load Pixelify font if not loaded
        if (pixelifyFont == null)
        {
            pixelifyFont = Resources.Load<Font>("Fonts/PixelifySans");
            if (pixelifyFont == null)
            {
                // Try alternate paths
                pixelifyFont = Resources.Load<Font>("PixelifySans");
            }
        }

        // Initialize style
        if (textStyle == null)
        {
            textStyle = new GUIStyle();
            textStyle.fontSize = 48;
            textStyle.fontStyle = FontStyle.Italic;
            textStyle.alignment = TextAnchor.MiddleCenter;
            textStyle.normal.textColor = Color.white;
            if (pixelifyFont != null) textStyle.font = pixelifyFont;
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
