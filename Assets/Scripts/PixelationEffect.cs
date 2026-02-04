using UnityEngine;

/// <summary>
/// Sea of Stars Pixelation Effect - ENHANCED v2
/// Features: Dynamic Resolution, Aspect Ratio Lock, Fixed Scanlines, Secondary Display Camera
/// FIXES: "No cameras rendering" error, scanline overlap issue
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class PixelationEffect : MonoBehaviour
{
    [Header("Resolution")]
    [Tooltip("Base pixel width (480 for classic SNES feel)")]
    public int targetWidth = 480;
    [Tooltip("Automatically calculate height from aspect ratio")]
    public bool lockAspectRatio = true;
    public FilterMode filterMode = FilterMode.Point;

    [Header("Visual Enhancements")]
    [Range(0f, 0.5f)] public float scanlineIntensity = 0.08f;
    [Tooltip("Scanline spacing (1 = every pixel, 2 = every other)")]
    [Range(1, 4)] public int scanlineSpacing = 2;
    public bool enableVignette = true;
    [Range(0f, 1f)] public float vignetteIntensity = 0.3f;

    [Header("Camera Fix")]
    [Tooltip("Create a display camera to prevent 'no cameras rendering' error")]
    public bool autoCreateDisplayCamera = true;

    [Header("Debug")]
    public bool showDebugInfo = false;

    private RenderTexture renderTex;
    private Camera cam;
    private Camera displayCam;
    private int calculatedHeight;
    private Texture2D scanlineTexture;

    void Start() => Initialize();
    void OnEnable() => Initialize();

    void Initialize()
    {
        cam = GetComponent<Camera>();

        // Calculate height from aspect ratio
        if (lockAspectRatio)
        {
            float aspectRatio = (float)Screen.width / Screen.height;
            calculatedHeight = Mathf.RoundToInt(targetWidth / aspectRatio);
        }
        else
        {
            calculatedHeight = 270;
        }

        // Release old texture
        if (renderTex != null) renderTex.Release();

        // Create new render texture
        renderTex = new RenderTexture(targetWidth, calculatedHeight, 24, RenderTextureFormat.ARGB32);
        renderTex.filterMode = filterMode;
        renderTex.antiAliasing = 1;
        renderTex.useMipMap = false;

        cam.targetTexture = renderTex;

        // FIX: Create a secondary camera to prevent "no cameras rendering" error
        if (autoCreateDisplayCamera)
        {
            CreateDisplayCamera();
        }

        // Create optimized scanline texture
        CreateScanlineTexture();
    }

    void CreateDisplayCamera()
    {
        // Check if display camera already exists
        var existing = transform.Find("DisplayCamera");
        if (existing != null)
        {
            displayCam = existing.GetComponent<Camera>();
            return;
        }

        // Create new display camera
        GameObject displayCamObj = new GameObject("DisplayCamera");
        displayCamObj.transform.SetParent(transform);
        displayCamObj.transform.localPosition = Vector3.zero;

        displayCam = displayCamObj.AddComponent<Camera>();
        displayCam.clearFlags = CameraClearFlags.Nothing;
        displayCam.cullingMask = 0; // Don't render any layers
        displayCam.depth = cam.depth - 1; // Render before main camera
        displayCam.orthographic = true;
    }

    void CreateScanlineTexture()
    {
        // Create a small repeating scanline texture (much more efficient than GUI.DrawTexture loops)
        int texHeight = scanlineSpacing * 2;
        scanlineTexture = new Texture2D(1, texHeight, TextureFormat.RGBA32, false);
        scanlineTexture.filterMode = FilterMode.Point;
        scanlineTexture.wrapMode = TextureWrapMode.Repeat;

        for (int y = 0; y < texHeight; y++)
        {
            // Dark line every 'scanlineSpacing' pixels
            bool isDarkLine = (y % scanlineSpacing) == 0 && y < scanlineSpacing;
            Color c = isDarkLine ? new Color(0, 0, 0, scanlineIntensity) : Color.clear;
            scanlineTexture.SetPixel(0, y, c);
        }
        scanlineTexture.Apply();
    }

    void OnDisable()
    {
        if (cam != null) cam.targetTexture = null;
        if (renderTex != null) renderTex.Release();
        if (displayCam != null) DestroyImmediate(displayCam.gameObject);
    }

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;
        if (renderTex == null) return;

        // Draw the pixelated render texture to fill screen
        Graphics.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), renderTex);

        // Overlay Scanlines (using optimized texture)
        if (scanlineIntensity > 0f && scanlineTexture != null)
        {
            DrawScanlines();
        }

        // Overlay Vignette
        if (enableVignette && vignetteIntensity > 0f)
        {
            DrawVignette();
        }

        // Debug Info
        if (showDebugInfo)
        {
            GUI.Label(new Rect(10, 10, 300, 60),
                $"Pixelation: {targetWidth}x{calculatedHeight}\n" +
                $"Screen: {Screen.width}x{Screen.height}\n" +
                $"Scale: {(float)Screen.height / calculatedHeight:F1}x");
        }
    }

    void DrawScanlines()
    {
        // FIX: Use tiled texture instead of per-line drawing (prevents overlap)
        float lineHeight = (float)Screen.height / calculatedHeight;
        Rect screenRect = new Rect(0, 0, Screen.width, Screen.height);

        // Calculate UV scaling for proper tiling
        float uvScaleY = calculatedHeight / (float)(scanlineSpacing * 2);

        GUI.DrawTextureWithTexCoords(
            screenRect,
            scanlineTexture,
            new Rect(0, 0, 1, uvScaleY)
        );
    }

    void DrawVignette()
    {
        float edge = Screen.width * vignetteIntensity * 0.5f;
        Color vignetteColor = new Color(0, 0, 0, vignetteIntensity * 0.5f);

        // Top
        GUI.DrawTexture(new Rect(0, 0, Screen.width, edge), Texture2D.whiteTexture,
            ScaleMode.StretchToFill, true, 0, vignetteColor, 0, 0);
        // Bottom
        GUI.DrawTexture(new Rect(0, Screen.height - edge, Screen.width, edge), Texture2D.whiteTexture,
            ScaleMode.StretchToFill, true, 0, vignetteColor, 0, 0);
        // Left
        GUI.DrawTexture(new Rect(0, 0, edge, Screen.height), Texture2D.whiteTexture,
            ScaleMode.StretchToFill, true, 0, vignetteColor, 0, 0);
        // Right
        GUI.DrawTexture(new Rect(Screen.width - edge, 0, edge, Screen.height), Texture2D.whiteTexture,
            ScaleMode.StretchToFill, true, 0, vignetteColor, 0, 0);
    }

    /// <summary>
    /// Call this to change resolution at runtime (e.g., for "Dream Sequence" effect)
    /// </summary>
    public void SetResolution(int width)
    {
        targetWidth = width;
        Initialize();
    }
}
