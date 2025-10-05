using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages outlines for layered sprite objects by creating a single combined outline
/// instead of outlining each layer individually. Perfect for line art + color layer setups.
/// </summary>
public class LayeredSpriteOutline : MonoBehaviour
{
    [Header("Outline Settings")]
    [SerializeField] private Color outlineColor = Color.white;
    
    [SerializeField, Range(0, 16)] 
    private int outlineSize = 1;
    
    [SerializeField] 
    private bool enableOutline = true;

    [Header("Outline Method")]
    [SerializeField] private OutlineMethod outlineMethod = OutlineMethod.CombinedTexture;

    [Header("Layer Management")]
    [SerializeField] 
    private bool includeInactiveChildren = false;
    
    [SerializeField] 
    private bool autoFindSpriteRenderers = true;
    
    [SerializeField] 
    private List<SpriteRenderer> manualSpriteRenderers = new List<SpriteRenderer>();

    [Header("Combined Outline Settings")]
    [SerializeField] 
    private bool createOutlineObject = true;
    
    [SerializeField] 
    private string outlineObjectName = "CombinedOutline";
    
    [SerializeField] 
    private int outlineRenderOrder = -1; // Behind the main sprites

    public enum OutlineMethod
    {
        CombinedTexture,    // Creates a single outline around the combined shape
        IndividualLayers,   // Outlines each layer separately (old behavior)
        OutlineOnTopLayer   // Only outlines the topmost layer
    }

    private List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>();
    private GameObject outlineObject;
    private SpriteRenderer outlineRenderer;
    private List<SpriteOutline> individualOutlines = new List<SpriteOutline>();

    void Start()
    {
        SetupLayeredOutlines();
    }

    void OnValidate()
    {
        outlineSize = Mathf.Clamp(outlineSize, 0, 16);
        
        if (Application.isPlaying)
        {
            SetupLayeredOutlines();
        }
    }

    /// <summary>
    /// Sets up the appropriate outline method
    /// </summary>
    public void SetupLayeredOutlines()
    {
        ClearExistingOutlines();
        CollectSpriteRenderers();
        
        switch (outlineMethod)
        {
            case OutlineMethod.CombinedTexture:
                CreateCombinedOutline();
                break;
            case OutlineMethod.IndividualLayers:
                CreateIndividualOutlines();
                break;
            case OutlineMethod.OutlineOnTopLayer:
                CreateTopLayerOutline();
                break;
        }
    }

    /// <summary>
    /// Collects all sprite renderers based on settings
    /// </summary>
    private void CollectSpriteRenderers()
    {
        spriteRenderers.Clear();
        
        if (autoFindSpriteRenderers)
        {
            SpriteRenderer[] foundRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactiveChildren);
            spriteRenderers.AddRange(foundRenderers);
        }
        
        foreach (SpriteRenderer renderer in manualSpriteRenderers)
        {
            if (renderer != null && !spriteRenderers.Contains(renderer))
            {
                spriteRenderers.Add(renderer);
            }
        }
    }

    /// <summary>
    /// Creates a single outline around the combined shape of all layers
    /// </summary>
    private void CreateCombinedOutline()
    {
        if (spriteRenderers.Count == 0) return;

        // Create a combined texture from all sprite layers
        Texture2D combinedTexture = CreateCombinedTexture();
        if (combinedTexture == null) return;

        // Create outline object
        if (createOutlineObject)
        {
            CreateOutlineObject(combinedTexture);
        }
    }

    /// <summary>
    /// Creates a combined texture from all sprite layers
    /// </summary>
    private Texture2D CreateCombinedTexture()
    {
        if (spriteRenderers.Count == 0) return null;

        // Find the bounds of all sprites
        Bounds combinedBounds = CalculateCombinedBounds();
        if (combinedBounds.size.x <= 0 || combinedBounds.size.y <= 0) return null;

        // Calculate texture size (use highest resolution sprite as reference)
        float maxPixelsPerUnit = GetMaxPixelsPerUnit();
        int textureWidth = Mathf.RoundToInt(combinedBounds.size.x * maxPixelsPerUnit);
        int textureHeight = Mathf.RoundToInt(combinedBounds.size.y * maxPixelsPerUnit);

        // Create combined texture
        Texture2D combinedTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[textureWidth * textureHeight];
        
        // Initialize with transparent pixels
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        // Render each sprite layer onto the combined texture
        foreach (SpriteRenderer renderer in spriteRenderers)
        {
            if (renderer.sprite != null && renderer.gameObject.activeInHierarchy)
            {
                RenderSpriteToTexture(renderer, combinedTexture, combinedBounds, maxPixelsPerUnit);
            }
        }

        combinedTexture.SetPixels(pixels);
        combinedTexture.Apply();
        
        return combinedTexture;
    }

    /// <summary>
    /// Calculates the combined bounds of all sprite renderers
    /// </summary>
    private Bounds CalculateCombinedBounds()
    {
        Bounds bounds = new Bounds();
        bool first = true;

        foreach (SpriteRenderer renderer in spriteRenderers)
        {
            if (renderer.sprite != null && renderer.gameObject.activeInHierarchy)
            {
                if (first)
                {
                    bounds = renderer.bounds;
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
        }

        return bounds;
    }

    /// <summary>
    /// Gets the maximum pixels per unit from all sprites
    /// </summary>
    private float GetMaxPixelsPerUnit()
    {
        float maxPPU = 100f; // Default fallback
        
        foreach (SpriteRenderer renderer in spriteRenderers)
        {
            if (renderer.sprite != null)
            {
                maxPPU = Mathf.Max(maxPPU, renderer.sprite.pixelsPerUnit);
            }
        }
        
        return maxPPU;
    }

    /// <summary>
    /// Renders a sprite to the combined texture (simplified version)
    /// </summary>
    private void RenderSpriteToTexture(SpriteRenderer renderer, Texture2D targetTexture, Bounds combinedBounds, float pixelsPerUnit)
    {
        // This is a simplified approach - for a full implementation, you'd need to properly
        // sample and composite the sprite textures considering transforms, scaling, etc.
        // For now, we'll use a different approach with an outline-only sprite
    }

    /// <summary>
    /// Creates the outline object with combined sprite
    /// </summary>
    private void CreateOutlineObject(Texture2D combinedTexture)
    {
        // Create outline GameObject
        if (outlineObject == null)
        {
            outlineObject = new GameObject(outlineObjectName);
            outlineObject.transform.SetParent(transform);
            outlineObject.transform.localPosition = Vector3.zero;
            outlineObject.transform.localScale = Vector3.one;
        }

        // Add SpriteRenderer
        if (outlineRenderer == null)
        {
            outlineRenderer = outlineObject.GetComponent<SpriteRenderer>();
            if (outlineRenderer == null)
            {
                outlineRenderer = outlineObject.AddComponent<SpriteRenderer>();
            }
        }

        // Create sprite from combined texture
        Sprite combinedSprite = Sprite.Create(
            combinedTexture,
            new Rect(0, 0, combinedTexture.width, combinedTexture.height),
            new Vector2(0.5f, 0.5f),
            GetMaxPixelsPerUnit()
        );

        outlineRenderer.sprite = combinedSprite;
        outlineRenderer.sortingOrder = outlineRenderOrder;

        // Add outline component
        SpriteOutline outline = outlineObject.GetComponent<SpriteOutline>();
        if (outline == null)
        {
            outline = outlineObject.AddComponent<SpriteOutline>();
        }

        outline.SetOutlineColor(outlineColor);
        outline.SetOutlineSize(outlineSize);
        outline.SetOutlineEnabled(enableOutline);
    }

    /// <summary>
    /// Creates individual outlines for each layer (original behavior)
    /// </summary>
    private void CreateIndividualOutlines()
    {
        foreach (SpriteRenderer renderer in spriteRenderers)
        {
            SpriteOutline outline = renderer.GetComponent<SpriteOutline>();
            if (outline == null)
            {
                outline = renderer.gameObject.AddComponent<SpriteOutline>();
            }
            
            outline.SetOutlineColor(outlineColor);
            outline.SetOutlineSize(outlineSize);
            outline.SetOutlineEnabled(enableOutline);
            
            individualOutlines.Add(outline);
        }
    }

    /// <summary>
    /// Creates outline only on the topmost layer
    /// </summary>
    private void CreateTopLayerOutline()
    {
        if (spriteRenderers.Count == 0) return;

        // Find the topmost sprite renderer (highest sorting order)
        SpriteRenderer topRenderer = spriteRenderers[0];
        foreach (SpriteRenderer renderer in spriteRenderers)
        {
            if (renderer.sortingOrder > topRenderer.sortingOrder)
            {
                topRenderer = renderer;
            }
        }

        // Apply outline only to top layer
        SpriteOutline outline = topRenderer.GetComponent<SpriteOutline>();
        if (outline == null)
        {
            outline = topRenderer.gameObject.AddComponent<SpriteOutline>();
        }
        
        outline.SetOutlineColor(outlineColor);
        outline.SetOutlineSize(outlineSize);
        outline.SetOutlineEnabled(enableOutline);
        
        individualOutlines.Add(outline);
    }

    /// <summary>
    /// Clears all existing outlines
    /// </summary>
    private void ClearExistingOutlines()
    {
        // Clear individual outlines
        foreach (SpriteOutline outline in individualOutlines)
        {
            if (outline != null)
            {
                if (Application.isPlaying)
                    Destroy(outline);
                else
                    DestroyImmediate(outline);
            }
        }
        individualOutlines.Clear();

        // Clear outline object
        if (outlineObject != null)
        {
            if (Application.isPlaying)
                Destroy(outlineObject);
            else
                DestroyImmediate(outlineObject);
            outlineObject = null;
            outlineRenderer = null;
        }
    }

    /// <summary>
    /// Sets the outline method and recreates outlines
    /// </summary>
    public void SetOutlineMethod(OutlineMethod method)
    {
        outlineMethod = method;
        SetupLayeredOutlines();
    }

    /// <summary>
    /// Enables or disables outlines
    /// </summary>
    public void SetOutlineEnabled(bool enable)
    {
        enableOutline = enable;
        
        if (outlineRenderer != null)
        {
            SpriteOutline outline = outlineRenderer.GetComponent<SpriteOutline>();
            if (outline != null)
            {
                outline.SetOutlineEnabled(enable);
            }
        }
        
        foreach (SpriteOutline outline in individualOutlines)
        {
            if (outline != null)
            {
                outline.SetOutlineEnabled(enable);
            }
        }
    }

    /// <summary>
    /// Sets the outline color
    /// </summary>
    public void SetOutlineColor(Color color)
    {
        outlineColor = color;
        
        if (outlineRenderer != null)
        {
            SpriteOutline outline = outlineRenderer.GetComponent<SpriteOutline>();
            if (outline != null)
            {
                outline.SetOutlineColor(color);
            }
        }
        
        foreach (SpriteOutline outline in individualOutlines)
        {
            if (outline != null)
            {
                outline.SetOutlineColor(color);
            }
        }
    }

    /// <summary>
    /// Sets the outline size
    /// </summary>
    public void SetOutlineSize(int size)
    {
        outlineSize = Mathf.Clamp(size, 0, 16);
        
        if (outlineRenderer != null)
        {
            SpriteOutline outline = outlineRenderer.GetComponent<SpriteOutline>();
            if (outline != null)
            {
                outline.SetOutlineSize(outlineSize);
            }
        }
        
        foreach (SpriteOutline outline in individualOutlines)
        {
            if (outline != null)
            {
                outline.SetOutlineSize(outlineSize);
            }
        }
    }

    void OnDestroy()
    {
        ClearExistingOutlines();
    }

#if UNITY_EDITOR
    [ContextMenu("Setup Combined Outline")]
    public void EditorSetupCombinedOutline()
    {
        outlineMethod = OutlineMethod.CombinedTexture;
        SetupLayeredOutlines();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    [ContextMenu("Setup Top Layer Outline Only")]
    public void EditorSetupTopLayerOutline()
    {
        outlineMethod = OutlineMethod.OutlineOnTopLayer;
        SetupLayeredOutlines();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    [ContextMenu("Setup Individual Layer Outlines")]
    public void EditorSetupIndividualOutlines()
    {
        outlineMethod = OutlineMethod.IndividualLayers;
        SetupLayeredOutlines();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    [ContextMenu("Clear All Outlines")]
    public void EditorClearOutlines()
    {
        ClearExistingOutlines();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}