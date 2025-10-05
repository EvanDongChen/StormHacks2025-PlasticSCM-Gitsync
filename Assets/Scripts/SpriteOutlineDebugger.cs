using UnityEngine;

/// <summary>
/// Debug utility to help troubleshoot SpriteOutline issues
/// </summary>
[RequireComponent(typeof(SpriteOutline))]
public class SpriteOutlineDebugger : MonoBehaviour
{
    [Header("Debug Info")]
    [SerializeField] private bool showDebugInfo = true;
    
    private SpriteOutline spriteOutline;
    private SpriteRenderer spriteRenderer;
    
    void Start()
    {
        spriteOutline = GetComponent<SpriteOutline>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (showDebugInfo)
        {
            DebugOutlineSetup();
        }
    }
    
    void DebugOutlineSetup()
    {
        Debug.Log("=== SpriteOutline Debug Info ===", this);
        
        // Check SpriteRenderer
        if (spriteRenderer == null)
        {
            Debug.LogError("No SpriteRenderer found!", this);
            return;
        }
        Debug.Log($"SpriteRenderer: {spriteRenderer.name}", this);
        Debug.Log($"Sprite: {(spriteRenderer.sprite ? spriteRenderer.sprite.name : "NULL")}", this);
        
        // Check Material
        Material mat = spriteRenderer.material;
        Debug.Log($"Current Material: {(mat ? mat.name : "NULL")}", this);
        Debug.Log($"Current Shader: {(mat && mat.shader ? mat.shader.name : "NULL")}", this);
        
        // Check if outline shader exists
        Shader outlineShader = Shader.Find("Sprites/Outline");
        if (outlineShader == null)
        {
            Debug.LogError("Outline shader 'Sprites/Outline' not found! Check if SpritesOutline.shader is in the project.", this);
        }
        else
        {
            Debug.Log("Outline shader found successfully!", this);
        }
        
        // Check outline settings
        Debug.Log($"Outline Enabled: {spriteOutline.IsOutlineEnabled()}", this);
        Debug.Log($"Outline Size: {spriteOutline.GetOutlineSize()}", this);
        Debug.Log($"Outline Color: {spriteOutline.GetOutlineColor()}", this);
        
        // Check material property block
        CheckMaterialPropertyBlock();
    }
    
    void CheckMaterialPropertyBlock()
    {
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(mpb);
        
        // We can't directly read from MaterialPropertyBlock, but we can set debug values
        Debug.Log("Material Property Block applied to SpriteRenderer", this);
    }
    
    [ContextMenu("Force Setup Outline")]
    public void ForceSetupOutline()
    {
        if (spriteOutline != null)
        {
            spriteOutline.SetupOutlineMaterial();
            spriteOutline.SetOutlineEnabled(true);
            spriteOutline.SetOutlineSize(3);
            spriteOutline.SetOutlineColor(Color.white);
            Debug.Log("Forced outline setup completed!", this);
        }
    }
    
    [ContextMenu("Test Different Colors")]
    public void TestDifferentColors()
    {
        if (spriteOutline != null)
        {
            StartCoroutine(ColorTestCoroutine());
        }
    }
    
    private System.Collections.IEnumerator ColorTestCoroutine()
    {
        Color[] testColors = { Color.white, Color.red, Color.green, Color.blue, Color.yellow, Color.magenta };
        
        foreach (Color color in testColors)
        {
            spriteOutline.SetOutlineColor(color);
            Debug.Log($"Testing outline color: {color}", this);
            yield return new WaitForSeconds(1f);
        }
    }
    
    [ContextMenu("Test Different Sizes")]
    public void TestDifferentSizes()
    {
        if (spriteOutline != null)
        {
            StartCoroutine(SizeTestCoroutine());
        }
    }
    
    private System.Collections.IEnumerator SizeTestCoroutine()
    {
        for (int size = 1; size <= 8; size++)
        {
            spriteOutline.SetOutlineSize(size);
            Debug.Log($"Testing outline size: {size}", this);
            yield return new WaitForSeconds(0.5f);
        }
    }
}