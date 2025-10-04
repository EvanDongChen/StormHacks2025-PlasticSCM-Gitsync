using UnityEngine;
using TMPro;
using System.Collections;

public class DogController : MonoBehaviour
{
    [Header("Dog Info")]
    public PlayerData playerData;
    
    [Header("Dog Components")]
    public TextMeshPro nameLabel;
    public TextMeshPro healthLabel;
    public Renderer dogRenderer; // This will be the BottomCoat renderer for coloring
    public Renderer topCoatRenderer; // For animations
    public Renderer bottomCoatRenderer; // For coloring and animations
    
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
        
        // Setup name label
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
            // Create name label if it doesn't exist
            CreateNameLabel();
        }
        
        // Setup health label
        if (healthLabel == null)
        {
            CreateHealthLabel();
        }
    }
    
    void CreateNameLabel()
    {
        GameObject labelObj = new GameObject("NameLabel");
        labelObj.transform.SetParent(transform);
        labelObj.transform.localPosition = new Vector3(0, 2, 0);
        
        nameLabel = labelObj.AddComponent<TextMeshPro>();
        nameLabel.text = playerData.playerName;
        nameLabel.fontSize = 2;
        nameLabel.alignment = TextAlignmentOptions.Center;
        nameLabel.color = Color.white;
    }
    
    void CreateHealthLabel()
    {
        GameObject labelObj = new GameObject("HealthLabel");
        labelObj.transform.SetParent(transform);
        labelObj.transform.localPosition = new Vector3(0, 1.5f, 0);
        
        healthLabel = labelObj.AddComponent<TextMeshPro>();
        healthLabel.fontSize = 1.5f;
        healthLabel.alignment = TextAlignmentOptions.Center;
        healthLabel.color = Color.green;
    }
    
    void UpdateHealthLabel()
    {
        if (healthLabel != null && playerData != null)
        {
            healthLabel.text = $"HP: {playerData.health}";
            
            // Change color based on health
            if (playerData.health > 70)
                healthLabel.color = Color.green;
            else if (playerData.health > 30)
                healthLabel.color = Color.yellow;
            else
                healthLabel.color = Color.red;
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
}