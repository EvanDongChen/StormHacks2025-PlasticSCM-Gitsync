using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class LobbyScript : MonoBehaviour
{
    [Header("UI References")]
    public Button createLobbyButton;
    public TextMeshProUGUI lobbyCodeDisplay;
    public TextMeshProUGUI connectionStatusText;
    public NetworkScript networkScript;
    
    [Header("Fade Effect")]
    public Image fadeImage; // Assign a black image that covers the screen
    public float fadeDuration = 1.0f;
    public string gameSceneName = "GameScene"; // Name of your game scene

    void Start()
    {
        // Auto-find NetworkScript if not assigned
        if (networkScript == null)
        {
            networkScript = FindFirstObjectByType<NetworkScript>();
        }

        // Assign UI references to NetworkScript
        if (networkScript != null)
        {
            networkScript.createLobbyButton = createLobbyButton;
            networkScript.lobbyCodeDisplay = lobbyCodeDisplay;
            networkScript.connectionStatusText = connectionStatusText;
        }
        else
        {
            Debug.LogError("NetworkScript not found! Make sure NetworkScript is in the scene.");
        }
        
        // Initialize fade image to be transparent
        if (fadeImage != null)
        {
            Color fadeColor = fadeImage.color;
            fadeColor.a = 0f; // Start transparent
            fadeImage.color = fadeColor;
            fadeImage.gameObject.SetActive(false); // Start hidden
        }
    }
    
    /// <summary>
    /// Call this method to start the fade to black transition to the game scene
    /// </summary>
    public void StartGameWithFade()
    {
        StartCoroutine(FadeToGameScene());
    }
    
    /// <summary>
    /// Coroutine that handles the fade to black effect and scene transition
    /// </summary>
    private IEnumerator FadeToGameScene()
    {
        Debug.Log("Starting fade to black...");
        
        if (fadeImage == null)
        {
            Debug.LogError("Fade image is null! Please assign a black Image UI element.");
            SceneManager.LoadScene(gameSceneName);
            yield break;
        }
        
        Debug.Log($"Fade image found: {fadeImage.name}");
        
        // Show the fade image and bring it to front
        fadeImage.gameObject.SetActive(true);
        fadeImage.transform.SetAsLastSibling(); // Ensure it's on top
        
        // Make sure the image is black
        Color fadeColor = Color.black;
        fadeColor.a = 0f; // Start transparent
        fadeImage.color = fadeColor;
        
        Debug.Log("Starting fade loop...");
        
        // Fade to black
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            
            fadeColor.a = alpha;
            fadeImage.color = fadeColor;
            
            // Debug every 0.2 seconds
            if (elapsedTime % 0.2f < Time.deltaTime)
            {
                Debug.Log($"Fade progress: {alpha:F2} (alpha)");
            }
            
            yield return null; // Wait for next frame
        }
        
        // Ensure we're fully black
        fadeColor.a = 1f;
        fadeImage.color = fadeColor;
        Debug.Log("Fade complete - screen should be black");
        
        yield return null; // Extra frame to ensure fade is visually complete
        
        // Optional: Add a small pause while fully black
        yield return new WaitForSeconds(0.1f);
        
        Debug.Log("Loading game scene...");
        
        // Now load the game scene
        SceneManager.LoadScene(gameSceneName);
    }
}