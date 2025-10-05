using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameUIController : MonoBehaviour
{
    [Header("Fade From Black")]
    public Image fadeImage; // Assign a black image that covers the screen
    public float fadeDuration = 1.0f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Start the fade from black effect
        if (fadeImage != null)
        {
            StartCoroutine(FadeFromBlack());
        }
        else
        {
            Debug.LogWarning("Fade image not assigned in GameUIController!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    /// <summary>
    /// Coroutine that handles the fade from black effect when the game scene starts
    /// </summary>
    private IEnumerator FadeFromBlack()
    {
        Debug.Log("Starting fade from black...");
        
        // Ensure the fade image is active and covers the screen
        fadeImage.gameObject.SetActive(true);
        fadeImage.transform.SetAsLastSibling(); // Ensure it's on top
        
        // Start with full black
        Color fadeColor = Color.black;
        fadeColor.a = 1f; // Start fully black
        fadeImage.color = fadeColor;
        
        Debug.Log("Fade image set to black, starting fade...");
        
        // Fade from black to transparent
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsedTime / fadeDuration); // Start at 1, go to 0
            
            fadeColor.a = alpha;
            fadeImage.color = fadeColor;
            
            // Debug every 0.2 seconds
            if (elapsedTime % 0.2f < Time.deltaTime)
            {
                Debug.Log($"Fade from black progress: {alpha:F2} (alpha)");
            }
            
            yield return null; // Wait for next frame
        }
        
        // Ensure we're fully transparent
        fadeColor.a = 0f;
        fadeImage.color = fadeColor;
        
        // Hide the fade image
        fadeImage.gameObject.SetActive(false);
        
        Debug.Log("Fade from black complete - game scene fully visible");
    }
}
