using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Component that combines all child sprites into one and adds outline effects using a custom shader.
/// Collects all child SpriteRenderer components, combines them into a single sprite, and applies outline.
/// </summary>
[ExecuteInEditMode]
public class SpriteOutline : MonoBehaviour 
{
    [Header("Outline Settings")]
    [SerializeField] private Color outlineColor = Color.white;
    
    [SerializeField, Range(0, 16)] 
    private int outlineSize = 1;
    
    [SerializeField] 
    private bool enableOutline = true;

    [Header("Auto-Setup")]
    [SerializeField] 
    private bool autoSetupMaterial = true;

    [Header("Sprite Combination")]
    [SerializeField]
    private bool autoUpdateOnChildChange = true;
    
    [SerializeField]
    private int textureSize = 1024; // Size of the combined texture

    private SpriteRenderer spriteRenderer;
    private Material outlineMaterial;
    private Material originalMaterial;
    private bool wasEnabled;
    private Sprite combinedSprite;
    private Texture2D combinedTexture;
    private List<SpriteRenderer> childSpriteRenderers = new List<SpriteRenderer>();

    // Property names for the shader
    private static readonly int OutlineProperty = Shader.PropertyToID("_Outline");
    private static readonly int OutlineColorProperty = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineSizeProperty = Shader.PropertyToID("_OutlineSize");

    void OnEnable() 
    {
        // Get or create SpriteRenderer component on this GameObject
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        CollectChildSprites();
        CombineSprites();
        SetupMaterial();
        UpdateOutline();
        wasEnabled = enableOutline;
    }

    void OnDisable() 
    {
        if (spriteRenderer != null)
        {
            UpdateOutline(false);
        }
    }

    void Update() 
    {
        // Check if outline state changed
        if (wasEnabled != enableOutline)
        {
            UpdateOutline();
            wasEnabled = enableOutline;
        }
        else if (enableOutline)
        {
            // Update outline properties if enabled
            UpdateOutline();
        }
    }

    void OnValidate()
    {
        // Ensure outline size is within valid range
        outlineSize = Mathf.Clamp(outlineSize, 0, 16);
        
        if (Application.isPlaying && spriteRenderer != null)
        {
            UpdateOutline();
        }
    }

    /// <summary>
    /// Sets up the outline material if auto-setup is enabled
    /// </summary>
    private void SetupMaterial()
    {
        if (!autoSetupMaterial) return;

        // Store original material
        if (originalMaterial == null)
        {
            originalMaterial = spriteRenderer.material;
        }

        // Create or find the outline material
        if (outlineMaterial == null)
        {
            Shader outlineShader = Shader.Find("Sprites/Outline");
            if (outlineShader != null)
            {
                outlineMaterial = new Material(outlineShader);
                outlineMaterial.name = "SpriteOutline Material";
            }
            else
            {
                Debug.LogError("Outline shader 'Sprites/Outline' not found! Make sure the SpritesOutline.shader is in your project.", this);
                return;
            }
        }

        spriteRenderer.material = outlineMaterial;
    }

    /// <summary>
    /// Updates the outline effect
    /// </summary>
    /// <param name="outline">Whether to enable or disable the outline</param>
    private void UpdateOutline(bool? outline = null)
    {
        if (spriteRenderer == null) return;

        bool shouldOutline = outline ?? enableOutline;

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(mpb);
        
        mpb.SetFloat(OutlineProperty, shouldOutline ? 1f : 0f);
        mpb.SetColor(OutlineColorProperty, outlineColor);
        mpb.SetFloat(OutlineSizeProperty, outlineSize);
        
        spriteRenderer.SetPropertyBlock(mpb);
    }

    /// <summary>
    /// Collects all SpriteRenderer components from child GameObjects
    /// </summary>
    private void CollectChildSprites()
    {
        childSpriteRenderers.Clear();
        SpriteRenderer[] childRenderers = GetComponentsInChildren<SpriteRenderer>();
        
        foreach (SpriteRenderer renderer in childRenderers)
        {
            // Skip the SpriteRenderer on this GameObject (the combined sprite renderer)
            if (renderer != spriteRenderer && renderer.sprite != null)
            {
                childSpriteRenderers.Add(renderer);
            }
        }
    }

    /// <summary>
    /// Combines all child sprites into a single sprite
    /// </summary>
    private void CombineSprites()
    {
        if (childSpriteRenderers.Count == 0)
        {
            Debug.LogWarning("No child sprites found to combine!", this);
            return;
        }

        // Calculate combined bounds
        Bounds combinedBounds = CalculateCombinedBounds();
        
        // Create render texture
        RenderTexture renderTexture = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGB32);
        renderTexture.Create();

        // Setup camera for rendering
        GameObject tempCameraGO = new GameObject("TempCamera");
        Camera tempCamera = tempCameraGO.AddComponent<Camera>();
        tempCamera.targetTexture = renderTexture;
        tempCamera.backgroundColor = Color.clear;
        tempCamera.clearFlags = CameraClearFlags.SolidColor;
        tempCamera.orthographic = true;
        tempCamera.orthographicSize = Mathf.Max(combinedBounds.size.x, combinedBounds.size.y) * 0.5f;
        tempCamera.transform.position = new Vector3(combinedBounds.center.x, combinedBounds.center.y, -10f);

        // Temporarily enable child renderers and render
        List<bool> originalStates = new List<bool>();
        foreach (SpriteRenderer renderer in childSpriteRenderers)
        {
            originalStates.Add(renderer.enabled);
            renderer.enabled = true;
        }

        tempCamera.Render();

        // Restore original states
        for (int i = 0; i < childSpriteRenderers.Count; i++)
        {
            childSpriteRenderers[i].enabled = originalStates[i];
        }

        // Convert to Texture2D
        RenderTexture.active = renderTexture;
        if (combinedTexture != null)
        {
            if (Application.isPlaying)
                Destroy(combinedTexture);
            else
                DestroyImmediate(combinedTexture);
        }
        
        combinedTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        combinedTexture.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);
        combinedTexture.Apply();
        RenderTexture.active = null;

        // Create sprite from texture
        if (combinedSprite != null)
        {
            if (Application.isPlaying)
                Destroy(combinedSprite);
            else
                DestroyImmediate(combinedSprite);
        }

        combinedSprite = Sprite.Create(combinedTexture, new Rect(0, 0, textureSize, textureSize), Vector2.one * 0.5f);
        spriteRenderer.sprite = combinedSprite;

        // Clean up
        tempCamera.targetTexture = null;
        renderTexture.Release();
        if (Application.isPlaying)
            Destroy(tempCameraGO);
        else
            DestroyImmediate(tempCameraGO);
    }

    /// <summary>
    /// Calculates the combined bounds of all child sprites
    /// </summary>
    private Bounds CalculateCombinedBounds()
    {
        if (childSpriteRenderers.Count == 0)
            return new Bounds();

        Bounds bounds = childSpriteRenderers[0].bounds;
        for (int i = 1; i < childSpriteRenderers.Count; i++)
        {
            bounds.Encapsulate(childSpriteRenderers[i].bounds);
        }
        return bounds;
    }

    /// <summary>
    /// Enables or disables the outline effect
    /// </summary>
    /// <param name="enable">True to enable outline, false to disable</param>
    public void SetOutlineEnabled(bool enable)
    {
        enableOutline = enable;
        UpdateOutline();
    }

    /// <summary>
    /// Sets the outline color
    /// </summary>
    /// <param name="color">The new outline color</param>
    public void SetOutlineColor(Color color)
    {
        outlineColor = color;
        if (enableOutline)
        {
            UpdateOutline();
        }
    }

    /// <summary>
    /// Sets the outline size
    /// </summary>
    /// <param name="size">Outline size (0-16)</param>
    public void SetOutlineSize(int size)
    {
        outlineSize = Mathf.Clamp(size, 0, 16);
        if (enableOutline)
        {
            UpdateOutline();
        }
    }

    /// <summary>
    /// Gets the current outline color
    /// </summary>
    public Color GetOutlineColor()
    {
        return outlineColor;
    }

    /// <summary>
    /// Gets the current outline size
    /// </summary>
    public int GetOutlineSize()
    {
        return outlineSize;
    }

    /// <summary>
    /// Returns true if outline is currently enabled
    /// </summary>
    public bool IsOutlineEnabled()
    {
        return enableOutline;
    }

    /// <summary>
    /// Manually setup the material (useful when auto-setup is disabled)
    /// </summary>
    public void SetupOutlineMaterial()
    {
        SetupMaterial();
    }

    /// <summary>
    /// Restore the original material
    /// </summary>
    public void RestoreOriginalMaterial()
    {
        if (spriteRenderer != null && originalMaterial != null)
        {
            spriteRenderer.material = originalMaterial;
        }
    }

    /// <summary>
    /// Manually recombine all child sprites
    /// </summary>
    public void RecombineSprites()
    {
        CollectChildSprites();
        CombineSprites();
        if (enableOutline)
        {
            UpdateOutline();
        }
    }

    /// <summary>
    /// Get the current combined sprite
    /// </summary>
    public Sprite GetCombinedSprite()
    {
        return combinedSprite;
    }

    /// <summary>
    /// Get the current combined texture
    /// </summary>
    public Texture2D GetCombinedTexture()
    {
        return combinedTexture;
    }

    void OnDestroy()
    {
        // Clean up created material
        if (outlineMaterial != null && autoSetupMaterial)
        {
            if (Application.isPlaying)
                Destroy(outlineMaterial);
            else
                DestroyImmediate(outlineMaterial);
        }

        // Clean up combined texture and sprite
        if (combinedTexture != null)
        {
            if (Application.isPlaying)
                Destroy(combinedTexture);
            else
                DestroyImmediate(combinedTexture);
        }

        if (combinedSprite != null)
        {
            if (Application.isPlaying)
                Destroy(combinedSprite);
            else
                DestroyImmediate(combinedSprite);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Setup Outline Material")]
    public void EditorSetupMaterial()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        
        CollectChildSprites();
        CombineSprites();
        SetupMaterial();
        UpdateOutline();
        
        // Mark dirty for editor
        UnityEditor.EditorUtility.SetDirty(this);
    }

    [ContextMenu("Recombine Child Sprites")]
    public void EditorRecombineSprites()
    {
        RecombineSprites();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    [ContextMenu("Restore Original Material")]
    public void EditorRestoreMaterial()
    {
        RestoreOriginalMaterial();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}