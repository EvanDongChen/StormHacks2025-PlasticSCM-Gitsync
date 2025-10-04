using UnityEngine;

public class StarTwinkle : MonoBehaviour
{
    [Header("Twinkling Settings")]
    public float twinkleSpeed = 2f;
    public float minAlpha = 0.3f;
    public float maxAlpha = 1f;
    public float minScale = 0.8f;
    public float maxScale = 1.2f;
    
    [Header("Animation Type")]
    public bool useAlphaTwinkle = true;
    public bool useScaleTwinkle = true;
    
    private SpriteRenderer spriteRenderer;
    private Transform starTransform;
    private Vector3 originalScale;
    private Color originalColor;
    private float timeOffset;
    
    void Start()
    {
        // Get components
        spriteRenderer = GetComponent<SpriteRenderer>();
        starTransform = transform;
        
        // Store original values
        originalScale = starTransform.localScale;
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        // Add random time offset for variety
        timeOffset = Random.Range(0f, 2f * Mathf.PI);
    }
    
    void Update()
    {
        float time = Time.time * twinkleSpeed + timeOffset;
        
        // Alpha twinkling
        if (useAlphaTwinkle && spriteRenderer != null)
        {
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(time) + 1f) / 2f);
            Color newColor = originalColor;
            newColor.a = alpha;
            spriteRenderer.color = newColor;
        }
        
        // Scale twinkling
        if (useScaleTwinkle)
        {
            float scaleMultiplier = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(time * 1.5f) + 1f) / 2f);
            starTransform.localScale = originalScale * scaleMultiplier;
        }
    }
}