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
    
    [Header("Star Lifecycle")]
    public float fadeInDuration = 2f; // Increased for more noticeable fade
    public float visibleDuration = 5f;
    public float fadeOutDuration = 1.5f; // Slightly longer fade out
    public bool enableRespawning = true;
    
    [Header("Fade Effects")]
    public AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public bool useSmoothFadeIn = true;
    
    [Header("Timing Variation")]
    public float visibleDurationVariation = 3f; // Random range to add/subtract from visible duration
    public float initialDelayMax = 2f; // Random delay before starting lifecycle
    
    // Lifecycle states
    private enum StarState { WaitingToStart, FadingIn, Visible, FadingOut }
    private StarState currentState = StarState.WaitingToStart;
    private float stateTimer = 0f;
    private float actualVisibleDuration; // Randomized visible duration for this star
    
    // Respawn settings (will be set by spawner)
    private float spawnMinX = -10f;
    private float spawnMaxX = 10f;
    private float spawnMinY = -5f;
    private float spawnMaxY = 5f;
    private float spawnZ = 0f;
    
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
            // Start invisible for fade in effect
            Color startColor = originalColor;
            startColor.a = 0f;
            spriteRenderer.color = startColor;
        }
        
        // Add random time offset for variety
        timeOffset = UnityEngine.Random.Range(0f, 2f * Mathf.PI);
        
        // Randomize visible duration for this star
        actualVisibleDuration = visibleDuration + UnityEngine.Random.Range(-visibleDurationVariation, visibleDurationVariation);
        actualVisibleDuration = Mathf.Max(1f, actualVisibleDuration); // Ensure minimum 1 second visible
        
        // Start with random delay to stagger star lifecycles
        float initialDelay = UnityEngine.Random.Range(0f, initialDelayMax);
        currentState = StarState.WaitingToStart;
        stateTimer = -initialDelay; // Negative timer for delay
    }
    
    // Method to set spawn ranges (called by StarSpawningScript)
    public void SetSpawnRanges(float minX, float maxX, float minY, float maxY, float z)
    {
        spawnMinX = minX;
        spawnMaxX = maxX;
        spawnMinY = minY;
        spawnMaxY = maxY;
        spawnZ = z;
    }
    
    void Update()
    {
        stateTimer += Time.deltaTime;
        
        switch (currentState)
        {
            case StarState.WaitingToStart:
                HandleWaitingToStart();
                break;
            case StarState.FadingIn:
                HandleFadeIn();
                break;
            case StarState.Visible:
                HandleVisible();
                break;
            case StarState.FadingOut:
                HandleFadeOut();
                break;
        }
    }
    
    private void HandleWaitingToStart()
    {
        // Stay invisible during delay
        if (spriteRenderer != null)
        {
            Color currentColor = originalColor;
            currentColor.a = 0f;
            spriteRenderer.color = currentColor;
        }
        
        // Transition to fade in after delay
        if (stateTimer >= 0f)
        {
            currentState = StarState.FadingIn;
            stateTimer = 0f;
        }
    }
    
    private void HandleFadeIn()
    {
        float fadeProgress = stateTimer / fadeInDuration;
        
        // Use smooth curve for more natural fade
        if (useSmoothFadeIn)
        {
            fadeProgress = fadeInCurve.Evaluate(fadeProgress);
        }
        
        if (spriteRenderer != null)
        {
            Color currentColor = originalColor;
            currentColor.a = Mathf.Lerp(0f, originalColor.a, fadeProgress);
            spriteRenderer.color = currentColor;
        }
        
        // Apply gentle scale animation during fade in - more dramatic effect
        if (useScaleTwinkle)
        {
            float scaleProgress = Mathf.Lerp(0.2f, 1f, fadeProgress); // Start smaller for more dramatic effect
            starTransform.localScale = originalScale * scaleProgress;
        }
        
        // Transition to visible state
        if (stateTimer >= fadeInDuration)
        {
            currentState = StarState.Visible;
            stateTimer = 0f;
        }
    }
    
    private void HandleVisible()
    {
        // Normal twinkling behavior when visible
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
        
        // Transition to fade out
        if (stateTimer >= actualVisibleDuration)
        {
            currentState = StarState.FadingOut;
            stateTimer = 0f;
        }
    }
    
    private void HandleFadeOut()
    {
        float fadeProgress = stateTimer / fadeOutDuration;
        
        // Use smooth curve for fade out too
        if (useSmoothFadeIn)
        {
            fadeProgress = fadeInCurve.Evaluate(fadeProgress);
        }
        
        if (spriteRenderer != null)
        {
            float currentAlpha = useAlphaTwinkle ? 
                Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(Time.time * twinkleSpeed + timeOffset) + 1f) / 2f) : 
                originalColor.a;
            
            Color currentColor = originalColor;
            currentColor.a = Mathf.Lerp(currentAlpha, 0f, fadeProgress);
            spriteRenderer.color = currentColor;
        }
        
        // Apply gentle scale animation during fade out - more dramatic shrinking
        if (useScaleTwinkle)
        {
            float scaleProgress = Mathf.Lerp(1f, 0.1f, fadeProgress); // Shrink more dramatically
            starTransform.localScale = originalScale * scaleProgress;
        }
        
        // Respawn or destroy
        if (stateTimer >= fadeOutDuration)
        {
            if (enableRespawning)
            {
                RespawnAtNewLocation();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
    
    private void RespawnAtNewLocation()
    {
        // Move to new random position
        float newX = UnityEngine.Random.Range(spawnMinX, spawnMaxX);
        float newY = UnityEngine.Random.Range(spawnMinY, spawnMaxY);
        starTransform.position = new Vector3(newX, newY, spawnZ);
        
        // Randomize visible duration for next cycle
        actualVisibleDuration = visibleDuration + UnityEngine.Random.Range(-visibleDurationVariation, visibleDurationVariation);
        actualVisibleDuration = Mathf.Max(1f, actualVisibleDuration);
        
        // Add random delay before starting next cycle
        float nextDelay = UnityEngine.Random.Range(0f, initialDelayMax * 0.5f); // Shorter delay for respawn
        currentState = StarState.WaitingToStart;
        stateTimer = -nextDelay;
        
        // Add new random time offset for variety
        timeOffset = UnityEngine.Random.Range(0f, 2f * Mathf.PI);
    }
}