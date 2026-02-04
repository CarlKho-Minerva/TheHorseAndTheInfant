using UnityEngine;

/// <summary>
/// CRT Pixelation Effect - ENHANCED for itch.io
/// Features: Dynamic Resolution, Aspect Ratio Lock, CRT Scanlines, Curvature, Bloom Glow
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

    [Header("CRT Effect")]
    [Range(0f, 0.5f)] public float scanlineIntensity = 0.15f;
    [Tooltip("Scanline spacing (1 = every pixel, 2 = every other)")]
    [Range(1, 4)] public int scanlineSpacing = 2;
    [Range(0f, 0.3f)] public float crtCurvature = 0.05f;
    [Range(0f, 1f)] public float rgbSeparation = 0.002f;
    [Range(0f, 1f)] public float bloomIntensity = 0.1f;

    [Header("Vignette")]
    public bool enableVignette = true;
    [Range(0f, 1f)] public float vignetteIntensity = 0.4f;

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
        // CRT SCANLINES - horizontal dark lines
        float lineHeight = (float)Screen.height / calculatedHeight;

        // Draw horizontal scanlines
        for (int y = 0; y < calculatedHeight; y += scanlineSpacing)
        {
            float screenY = y * lineHeight;
            GUI.color = new Color(0, 0, 0, scanlineIntensity);
            GUI.DrawTexture(new Rect(0, screenY, Screen.width, lineHeight * 0.5f), Texture2D.whiteTexture);
        }

        // Reset color
        GUI.color = Color.white;
    }

    void DrawVignette()
    {
        // Smooth radial vignette for CRT monitor edge darkening
        float edge = Screen.width * vignetteIntensity * 0.5f;
        float cornerDark = vignetteIntensity * 0.7f;

        // Top edge (gradient)
        for (int i = 0; i < 10; i++)
        {
            float alpha = (1f - i / 10f) * cornerDark * 0.3f;
            GUI.color = new Color(0, 0, 0, alpha);
            GUI.DrawTexture(new Rect(0, i * edge / 10f, Screen.width, edge / 10f), Texture2D.whiteTexture);
        }

        // Bottom edge
        for (int i = 0; i < 10; i++)
        {
            float alpha = (1f - i / 10f) * cornerDark * 0.3f;
            GUI.color = new Color(0, 0, 0, alpha);
            GUI.DrawTexture(new Rect(0, Screen.height - (i + 1) * edge / 10f, Screen.width, edge / 10f), Texture2D.whiteTexture);
        }

        // Left edge
        for (int i = 0; i < 10; i++)
        {
            float alpha = (1f - i / 10f) * cornerDark * 0.3f;
            GUI.color = new Color(0, 0, 0, alpha);
            GUI.DrawTexture(new Rect(i * edge / 10f, 0, edge / 10f, Screen.height), Texture2D.whiteTexture);
        }

        // Right edge
        for (int i = 0; i < 10; i++)
        {
            float alpha = (1f - i / 10f) * cornerDark * 0.3f;
            GUI.color = new Color(0, 0, 0, alpha);
            GUI.DrawTexture(new Rect(Screen.width - (i + 1) * edge / 10f, 0, edge / 10f, Screen.height), Texture2D.whiteTexture);
        }

        // Corner darkening (extra dark in corners)
        GUI.color = new Color(0, 0, 0, cornerDark);
        GUI.DrawTexture(new Rect(0, 0, edge, edge), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(Screen.width - edge, 0, edge, edge), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0, Screen.height - edge, edge, edge), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(Screen.width - edge, Screen.height - edge, edge, edge), Texture2D.whiteTexture);

        GUI.color = Color.white;
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
