using UnityEngine;
using TMPro;
using System.Collections;

public class DogController : MonoBehaviour
{
    [Header("Dog Info")]
    public PlayerData playerData;
    
    [Header("UI Child Objects")]
    public GameObject nameObject;  // Child GameObject with TextMeshPro component
    public GameObject healthObject; // Child GameObject where hearts will be spawned
    
    [Header("Dog Components")]
    public TextMeshPro nameLabel;
    public TextMeshPro healthLabel;
    public Renderer dogRenderer; // This will be the BottomCoat renderer for coloring
    public Renderer topCoatRenderer; // For animations
    public Renderer bottomCoatRenderer; // For coloring and animations
    
    [Header("Health Hearts")]
    public Sprite heartSprite; // Heart image to spawn for each health point
    public GameObject[] heartObjects = new GameObject[4]; // Array to hold 4 heart GameObjects
    public float heartSpacing = 0.3f; // Spacing between hearts
    
    [Header("Eye Sprites")]
    public SpriteRenderer eyesRenderer; // The SpriteRenderer that shows the eyes
    public Sprite eyesOpenSprite; // Sprite with eyes open
    public Sprite eyesClosedSprite; // Sprite with eyes closed
    
    [Header("Blinking Settings")]
    public float minBlinkInterval = 2f; // Minimum time between blinks
    public float maxBlinkInterval = 5f; // Maximum time between blinks
    public float blinkDuration = 0.15f; // How long eyes stay closed
    
    [Header("Dog Colors")]
    public Color[] playerColors = { 
        new Color(0.8f, 0.5f, 0.3f),    // Red/Orange Sesame (most common)
        new Color(0.9f, 0.8f, 0.6f),    // Cream/White
        new Color(0.4f, 0.3f, 0.2f),    // Black and Tan
        new Color(0.6f, 0.4f, 0.25f),   // Red Sesame
        new Color(0.7f, 0.6f, 0.4f),    // Sesame (mixed)
        new Color(0.95f, 0.9f, 0.85f)   // White
    };
    
    void Start()
    {
        // Find child GameObjects if not assigned
        if (nameObject == null)
        {
            Transform nameChild = transform.Find("Name");
            if (nameChild != null)
            {
                nameObject = nameChild.gameObject;
            }
        }
        
        if (healthObject == null)
        {
            Transform healthChild = transform.Find("Health");
            if (healthChild != null)
            {
                healthObject = healthChild.gameObject;
            }
        }
        
        SetupLabels();
        SetDogColor();
        SetupEyes();
        StartBlinking();
    }
    
    void Update()
    {
        UpdateHealthLabel();
    }
    
    public void SetPlayerData(PlayerData data)
    {
        playerData = data;
        SetupLabels();
        SetDogColor();
    }
    
    void SetupLabels()
    {
        if (playerData == null) return;
        
        // Setup name label using existing TextMeshPro component on Name child
        if (nameObject != null)
        {
            nameLabel = nameObject.GetComponent<TextMeshPro>();
            if (nameLabel != null)
            {
                nameLabel.text = playerData.playerName;
                if (playerData.isHost)
                {
                    nameLabel.text += " (Host)";
                }
            }
            else
            {
                Debug.LogWarning($"Name GameObject found but no TextMeshPro component attached on {playerData.playerName}'s dog!");
            }
        }
        else
        {
            Debug.LogWarning($"No Name child object found on {playerData.playerName}'s dog! Make sure the dog prefab has a 'Name' child GameObject with TextMeshPro component.");
        }
        
        // Setup health hearts instead of health bar
        SetupHealthHearts();
    }
    
    void CreateHealthLabel()
    {
        // This method is now replaced by SetupHealthHearts
        // Keeping for backward compatibility but not used
    }
    
    void SetupHealthHearts()
    {
        if (healthObject == null)
        {
            Debug.LogWarning("Health GameObject not found! Hearts will not be displayed.");
            return;
        }
        
        // Clear any existing hearts
        for (int i = 0; i < heartObjects.Length; i++)
        {
            if (heartObjects[i] != null)
            {
                DestroyImmediate(heartObjects[i]);
                heartObjects[i] = null;
            }
        }
        
        // Create 4 heart GameObjects
        for (int i = 0; i < 4; i++)
        {
            GameObject heartObj = new GameObject($"Heart_{i}");
            heartObj.transform.SetParent(healthObject.transform);
            
            // Position hearts horizontally with spacing
            heartObj.transform.localPosition = new Vector3((i - 1.5f) * heartSpacing, 0, 0);
            
            // Add SpriteRenderer component
            SpriteRenderer heartRenderer = heartObj.AddComponent<SpriteRenderer>();
            
            if (heartSprite != null)
            {
                heartRenderer.sprite = heartSprite;
            }
            else
            {
                // Create a simple heart-shaped sprite if none provided
                heartRenderer.sprite = CreateDefaultHeartSprite();
            }
            
            // Scale the heart appropriately
            heartObj.transform.localScale = Vector3.one * 0.5f;
            
            heartObjects[i] = heartObj;
        }
        
        // Update heart display based on current health
        UpdateHealthHearts();
    }
    
    Sprite CreateDefaultHeartSprite()
    {
        // Create a simple red square as a default heart
        Texture2D texture = new Texture2D(16, 16);
        Color heartColor = Color.red;
        
        for (int x = 0; x < 16; x++)
        {
            for (int y = 0; y < 16; y++)
            {
                texture.SetPixel(x, y, heartColor);
            }
        }
        
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
    }
    
    void UpdateHealthLabel()
    {
        // Update hearts instead of health label
        UpdateHealthHearts();
    }
    
    void UpdateHealthHearts()
    {
        if (heartObjects == null || playerData == null) return;
        
        // Convert health (0-100) to hearts (0-4)
        int activeHearts = Mathf.Clamp(playerData.health, 0, 4);
        
        for (int i = 0; i < heartObjects.Length; i++)
        {
            if (heartObjects[i] != null)
            {
                // Show hearts based on current health, hide hearts beyond current health
                heartObjects[i].SetActive(i < activeHearts);
            }
        }
    }
    
    void SetDogColor()
    {
        // Find the coat renderers if not already assigned
        if (bottomCoatRenderer == null)
        {
            Transform bottomCoatChild = transform.Find("BottomCoat");
            if (bottomCoatChild != null)
            {
                bottomCoatRenderer = bottomCoatChild.GetComponent<SpriteRenderer>();
            }
        }
        
        if (topCoatRenderer == null)
        {
            Transform topCoatChild = transform.Find("TopCoat");
            if (topCoatChild != null)
            {
                topCoatRenderer = topCoatChild.GetComponent<SpriteRenderer>();
            }
        }
        
        // Set dogRenderer to bottomCoatRenderer for backward compatibility
        if (dogRenderer == null)
            dogRenderer = bottomCoatRenderer;
            
        if (bottomCoatRenderer != null && playerData != null)
        {
            // Use player name hash to get consistent color - only apply to BottomCoat
            int colorIndex = Mathf.Abs(playerData.playerName.GetHashCode()) % playerColors.Length;
            bottomCoatRenderer.material.color = playerColors[colorIndex];
            
            // Make host dogs slightly bigger and brighter
            if (playerData.isHost)
            {
                transform.localScale = Vector3.one * 1.2f;
                bottomCoatRenderer.material.color = Color.Lerp(bottomCoatRenderer.material.color, Color.white, 0.3f);
            }
        }
    }
    
    void SetupEyes()
    {
        // Find the eyes renderer if not already assigned
        if (eyesRenderer == null)
        {
            Transform eyesChild = transform.Find("Eyes");
            if (eyesChild != null)
            {
                eyesRenderer = eyesChild.GetComponent<SpriteRenderer>();
            }
        }
        
        // Set initial sprite to eyes open
        if (eyesRenderer != null && eyesOpenSprite != null)
        {
            eyesRenderer.sprite = eyesOpenSprite;
        }
    }
    
    void StartBlinking()
    {
        if (eyesRenderer != null && eyesOpenSprite != null && eyesClosedSprite != null)
        {
            StartCoroutine(BlinkingCoroutine());
        }
    }
    
    IEnumerator BlinkingCoroutine()
    {
        while (true)
        {
            // Wait for a random interval between blinks
            float waitTime = UnityEngine.Random.Range(minBlinkInterval, maxBlinkInterval);
            yield return new WaitForSeconds(waitTime);
            
            // Blink (close eyes)
            if (eyesRenderer != null && eyesClosedSprite != null)
            {
                eyesRenderer.sprite = eyesClosedSprite;
                yield return new WaitForSeconds(blinkDuration);
                
                // Open eyes again
                if (eyesOpenSprite != null)
                {
                    eyesRenderer.sprite = eyesOpenSprite;
                }
            }
        }
    }
    
    // Animation methods
    public void Bark()
    {
        // Simple scale animation for barking using coroutine
        StartCoroutine(BarkAnimation());
    }
    
    public void Wag()
    {
        // Rotation animation for tail wagging
        StartCoroutine(WagAnimation());
    }
    
    public void TakeDamage()
    {
        // Red flash when taking damage
        StartCoroutine(DamageAnimation());
    }
    
    // Animation coroutines
    IEnumerator BarkAnimation()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * 1.1f;
        
        // Scale up
        float timer = 0f;
        while (timer < 0.1f)
        {
            timer += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, timer / 0.1f);
            yield return null;
        }
        
        // Scale back down
        timer = 0f;
        while (timer < 0.1f)
        {
            timer += Time.deltaTime;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, timer / 0.1f);
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
    
    IEnumerator WagAnimation()
    {
        Vector3 originalRotation = transform.eulerAngles;
        
        for (int i = 0; i < 3; i++)
        {
            // Rotate right
            float timer = 0f;
            while (timer < 0.2f)
            {
                timer += Time.deltaTime;
                float angle = Mathf.Lerp(0, 10f, timer / 0.2f);
                transform.eulerAngles = originalRotation + new Vector3(0, 0, angle);
                yield return null;
            }
            
            // Rotate left
            timer = 0f;
            while (timer < 0.2f)
            {
                timer += Time.deltaTime;
                float angle = Mathf.Lerp(10f, -10f, timer / 0.2f);
                transform.eulerAngles = originalRotation + new Vector3(0, 0, angle);
                yield return null;
            }
        }
        
        // Return to original rotation
        transform.eulerAngles = originalRotation;
    }
    
    IEnumerator DamageAnimation()
    {
        // Store original colors for both coats
        Color originalBottomColor = Color.white;
        Color originalTopColor = Color.white;
        
        if (bottomCoatRenderer != null)
            originalBottomColor = bottomCoatRenderer.material.color;
        if (topCoatRenderer != null)
            originalTopColor = topCoatRenderer.material.color;
        
        // Flash both coats red
        if (bottomCoatRenderer != null)
            bottomCoatRenderer.material.color = Color.red;
        if (topCoatRenderer != null)
            topCoatRenderer.material.color = Color.red;
            
        yield return new WaitForSeconds(0.1f);
        
        // Fade both coats back to original colors
        float timer = 0f;
        while (timer < 0.4f)
        {
            timer += Time.deltaTime;
            float lerpValue = timer / 0.4f;
            
            if (bottomCoatRenderer != null)
                bottomCoatRenderer.material.color = Color.Lerp(Color.red, originalBottomColor, lerpValue);
            if (topCoatRenderer != null)
                topCoatRenderer.material.color = Color.Lerp(Color.red, originalTopColor, lerpValue);
                
            yield return null;
        }
        
        // Ensure final colors are set correctly
        if (bottomCoatRenderer != null)
            bottomCoatRenderer.material.color = originalBottomColor;
        if (topCoatRenderer != null)
            topCoatRenderer.material.color = originalTopColor;
    }
    
    void OnMouseDown()
    {
        // Pet the dog when clicked
        Wag();
        Debug.Log($"You petted {playerData.playerName}'s dog!");
    }
    
    public void TriggerDamageEffect()
    {
        // Simple damage effect - could be enhanced with more complex animations
        StartCoroutine(DamageFlashEffect());
    }
    
    private IEnumerator DamageFlashEffect()
    {
        // Flash red briefly to indicate damage
        Color originalBottomColor = Color.white;
        Color originalTopColor = Color.white;
        
        if (bottomCoatRenderer != null)
            originalBottomColor = bottomCoatRenderer.material.color;
        if (topCoatRenderer != null)
            originalTopColor = topCoatRenderer.material.color;
        
        // Flash red
        if (bottomCoatRenderer != null)
            bottomCoatRenderer.material.color = Color.red;
        if (topCoatRenderer != null)
            topCoatRenderer.material.color = Color.red;
        
        yield return new WaitForSeconds(0.2f);
        
        // Return to original colors
        if (bottomCoatRenderer != null)
            bottomCoatRenderer.material.color = originalBottomColor;
        if (topCoatRenderer != null)
            topCoatRenderer.material.color = originalTopColor;
        
        Debug.Log($"{playerData.playerName}'s dog took damage!");
    }
    
    // Magical outline methods
    public void ApplyMagicalOutline()
    {
        // Check if we have a SpriteRenderer component to apply outline to
        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"No SpriteRenderer found on {playerData.playerName}'s dog for magical outline!");
            return;
        }
        
        
        Debug.Log($"{playerData.playerName}'s dog is now enchanted with magical effects!");
    }
    

}