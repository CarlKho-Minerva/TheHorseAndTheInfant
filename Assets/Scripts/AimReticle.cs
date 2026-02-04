using UnityEngine;

/// <summary>
/// Simple Aim Reticle
/// Draws a ring on the ground where the mouse is pointing
/// Works with PixelationEffect's RenderTexture camera setup
/// </summary>
public class AimReticle : MonoBehaviour
{
    [Header("Appearance")]
    public float radius = 1.0f;
    public Color color = Color.yellow;
    public float yOffset = 1.0f;  // Raised higher above ground
    public int segments = 32;
    public float lineWidth = 0.15f;

    [Header("Ground Detection")]
    public float groundY = 0f;

    private LineRenderer line;
    private Camera mainCam;

    void Start()
    {
        // Find the MAIN camera (the one with PixelationEffect that actually renders the scene)
        mainCam = Camera.main;
        if (mainCam == null)
        {
            // Fallback: find any camera tagged MainCamera or just any camera
            foreach (Camera c in FindObjectsOfType<Camera>())
            {
                if (c.CompareTag("MainCamera") || c.GetComponent<PixelationEffect>() != null)
                {
                    mainCam = c;
                    break;
                }
            }
        }
        if (mainCam == null)
        {
            mainCam = FindObjectOfType<Camera>();
        }
        if (mainCam == null)
        {
            Debug.LogError("[AimReticle] No Camera found!");
            return;
        }

        // Create LineRenderer
        line = GetComponent<LineRenderer>();
        if (line == null)
        {
            line = gameObject.AddComponent<LineRenderer>();
        }

        line.useWorldSpace = true;
        line.loop = true;
        line.positionCount = segments;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.sortingOrder = 100;

        // Use a reliable unlit shader
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = color;
        line.material = mat;
        line.startColor = color;
        line.endColor = color;

        UpdateCirclePosition(new Vector3(0, groundY + yOffset, 0));
        Debug.Log($"[AimReticle] Ready! Using camera: {mainCam.name}");
    }

    void Update()
    {
        if (mainCam == null || line == null) return;

        // Get mouse position
        Vector3 mousePos = Input.mousePosition;

        // If the camera renders to a RenderTexture (like PixelationEffect),
        // convert screen coords to render texture coords properly
        if (mainCam.targetTexture != null)
        {
            // Convert to normalized viewport coords (0-1), then to RT pixel coords
            float normalizedX = mousePos.x / Screen.width;
            float normalizedY = mousePos.y / Screen.height;
            mousePos.x = normalizedX * mainCam.targetTexture.width;
            mousePos.y = normalizedY * mainCam.targetTexture.height;
        }

        // Create ray from camera through mouse position
        Ray ray = mainCam.ScreenPointToRay(mousePos);

        // Raycast to ground plane
        Plane ground = new Plane(Vector3.up, new Vector3(0, groundY, 0));

        if (ground.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            hitPoint.y = groundY + yOffset;
            UpdateCirclePosition(hitPoint);
        }
    }

    void UpdateCirclePosition(Vector3 center)
    {
        if (line == null) return;

        float angleStep = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Sin(angle) * radius;
            float z = Mathf.Cos(angle) * radius;
            line.SetPosition(i, center + new Vector3(x, 0, z));
        }
    }
}
