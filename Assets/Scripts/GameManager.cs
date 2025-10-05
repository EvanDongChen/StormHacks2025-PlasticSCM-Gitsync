using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// Game state enums for socket events
public enum ServerGameState
{
    JOINING,
    PROMPT,
    GENERATING,
    TRIVIA,
    REWARD,
    ENDGAME
}

[System.Serializable]
public class TriviaData
{
    public string question;
    public string[] options = new string[4]; // A, B, C, D
    public int correctAnswerIndex;
    public PlayerData[] players; // Player data that comes with trivia state
    public float timer; // Round timer in seconds from server
}

[System.Serializable]
public class RewardData
{
    public PlayerResult[] results;
    public int solutionIndex;
    public PlayerData[] players;
    public float timer; // Reward phase timer in seconds from server
}

[System.Serializable]
public class PlayerResult
{
    public string playerId;
    public string playerName;
    public bool isCorrect;
    public int selectedAnswer;
}

[System.Serializable]
public class EndGameData
{
    public string[] winners;
    public PlayerData[] players; // Player data that comes with endgame state
}

[System.Serializable]
public class GameStateData
{
    public float timer; // General timer for game states like PROMPT
    public PlayerData[] players; // Player data for general state updates
}

[System.Serializable]
public class PlayerData
{
    public string playerName;
    public int health = 4;
    public bool isHost = false;
    public bool submitted = false;
    
    public PlayerData(string name, bool host = false)
    {
        playerName = name;
        isHost = host;
        health = 4;
        submitted = false;
    }
}

public enum GamePhase
{
    Joining,
    GamePhase,
    PointPhase
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Game Phases")]
    public GamePhase currentPhase = GamePhase.Joining;
    
    [Header("Server Game State")]
    public ServerGameState currentServerState = ServerGameState.JOINING;
    public TriviaData currentTriviaData;
    public RewardData currentRewardData;
    public EndGameData currentEndGameData;
    
    [Header("Game UI")]
    public TextMeshProUGUI lobbyCodeDisplay;
    public TextMeshProUGUI phaseDisplay;
    public TextMeshProUGUI roundTimerDisplay;      // Timer display for current round
    public Image timerIcon;                        // Timer icon to hide during endgame
    
    [Header("Host")]
    public HostScript hostScript;                   // Reference to the host script for state changes
    
    [Header("Trivia UI")]
    public GameObject promptWaitingPanel;          // "Players are choosing a category..."
    // public GameObject loadingPanel;                // "Generating Questions..." 
    // public GameObject triviaPanel;                 // Main trivia question display
    // public GameObject rewardPanel;                 // Damage/reward animation panel
    // public GameObject endGamePanel;                // Final winner display
    
    [Header("Trivia Question Components")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI answerAText;
    public TextMeshProUGUI answerBText;
    public TextMeshProUGUI answerCText;
    public TextMeshProUGUI answerDText;
    public TextMeshProUGUI loadingText;
    public TextMeshProUGUI endGameText;
    
    [Header("End Game UI")]
    public SpriteRenderer endGameDarkOverlay;      // Dark overlay GameObject with SpriteRenderer that covers everything except dogs
                                                  // Use a large black sprite and position it to cover the screen
    public TextMeshProUGUI winnerAnnouncementText; // "WE HAVE A WINNER" text (should be disabled by default)
    public Button mainMenuButton;                  // Button to return to main menu (should be disabled by default)
    public CanvasGroup endGameCanvasGroup;         // Canvas group for fading endgame elements
    public float endGameFadeDuration = 2.0f;       // How long the endgame fade takes
    public Color endGameOverlayColor = new Color(0, 0, 0, 0.9f); // Very dark overlay color
    public Vector3 centerScreenPosition = new Vector3(-3.5f, -1.5f, 0); // Position where winning dog should move to
    public float dogMoveDuration = 1.5f;           // How long it takes for the dog to move to center
    public float dogMoveDelay = 1.0f;              // Delay before moving dog to center
    
    [Header("Reward Effect Prefabs")]
    public GameObject starEffectPrefab;            // Star effect prefab for correct answers
    public GameObject bloodEffectPrefab;           // Blood effect prefab for incorrect answers
    public float effectDuration = 3.0f;           // How long effects should last
    public Vector3 effectOffset = Vector3.zero;   // Offset from dog position for effects
    
    [Header("Trivia UI Images")]
    public Image questionImage;           // Background image for question
    public Image answerAImage;            // Background image for answer A
    public Image answerBImage;            // Background image for answer B  
    public Image answerCImage;            // Background image for answer C
    public Image answerDImage;            // Background image for answer D
    
    [Header("UI Fade Settings")]
    public CanvasGroup promptPanelCanvasGroup;     // CanvasGroup for prompt panel fade
    public CanvasGroup triviaCanvasGroup;          // CanvasGroup for trivia elements fade
    public float fadeInDuration = 0.5f;            // How long fade in takes
    public float fadeOutDuration = 0.3f;           // How long fade out takes
    
    [Header("Phase Transition Settings")]
    public Image phaseTransitionDimImage;          // Image for dimming screen during phase transitions
    public TextMeshProUGUI phaseTransitionText;    // Text to display the phase name
    public float phaseDimDuration = 0.5f;          // How long to fade in the overlay
    public float phaseDisplayDuration = 3.0f;      // How long to show the phase text (total)
    public float phaseFadeOutDuration = 0.5f;      // How long to fade out the overlay
    
    [Header("Dynamic Font Settings")]
    public float minFontSize = 12f;                // Minimum font size
    public float maxFontSize = 48f;                // Maximum font size for questions
    public float maxAnswerFontSize = 32f;          // Maximum font size for answers
    public bool enableAutoSizing = true;           // Enable/disable auto-sizing
    public float fontSizePadding = 2f;             // Padding to ensure text doesn't touch edges
    
    // Animation state tracking
    private Coroutine currentFadeCoroutine;
    
    [Header("Phase Control")]
    public Button startGameButton;
    public Button nextPhaseButton;
    
    [Header("Player Data")]
    public List<PlayerData> playersData = new List<PlayerData>();
    
    [Header("Dog GameObjects")]
    public GameObject dogPrefab;              // Assign a dog prefab
    public Transform dogContainer;            // Parent object for organization
    public Vector3[] dogPositions;           // Predefined positions for dogs
    public List<GameObject> activeDogs = new List<GameObject>();
    
    [Header("Timer System")]
    public GameStateData currentGameStateData; // General game state data with timer
    private float currentTimerSeconds = 0f;    // Current timer value in seconds
    private bool isTimerActive = false;        // Whether the timer is currently counting down
    private Coroutine timerCoroutine;          // Reference to active timer coroutine
    
    private List<string> lastPlayerList = new List<string>();
    
    void Start()
    {
        // Set up singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple GameManager instances detected!");
        }
        
        // Set up button listeners
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(StartGame);
        }
        
        if (nextPhaseButton != null)
        {
            nextPhaseButton.onClick.AddListener(NextPhase);
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
        
        // Initialize phase (no transition on startup)
        currentPhase = GamePhase.Joining;
        UpdatePhaseUI();
        UpdateTimerVisibility(); // Initialize timer visibility based on current state
        Debug.Log($"Initial phase set to: {currentPhase}");
        
        // Initialize all game state panels to hidden
        HideAllGameStatePanels();
        ResetEndGameUI(); // Ensure endgame UI starts in clean state
        Debug.Log("All game state panels and trivia elements hidden on start");
        
        // Ensure all CanvasGroups start with alpha 0 (respecting editor settings)
        if (promptPanelCanvasGroup != null)
        {
            promptPanelCanvasGroup.alpha = 0f;
            promptPanelCanvasGroup.gameObject.SetActive(false);
            promptPanelCanvasGroup.interactable = false;
        }
        
        if (triviaCanvasGroup != null)
        {
            triviaCanvasGroup.alpha = 0f;
            triviaCanvasGroup.gameObject.SetActive(false);
            triviaCanvasGroup.interactable = false;
        }
        
        // Initialize phase transition overlay
        if (phaseTransitionDimImage != null)
        {
            Color imageColor = phaseTransitionDimImage.color;
            imageColor.a = 0f;
            phaseTransitionDimImage.color = imageColor;
            phaseTransitionDimImage.gameObject.SetActive(false);
        }
        
        // Initialize phase transition text
        if (phaseTransitionText != null)
        {
            phaseTransitionText.gameObject.SetActive(false);
        }
        
        // Setup dynamic font sizing for all text components
        if (enableAutoSizing)
        {
            SetupAllTextComponents();
        }
        
        // Ensure timer icon is enabled on start
        if (timerIcon != null)
        {
            timerIcon.gameObject.SetActive(true);
            Debug.Log("Timer icon enabled on start");
        }
        
        // Get the NetworkScript instance that persisted from the main menu
        if (NetworkScript.Instance != null)
        {
            DisplayLobbyInfo();
            // No need to initialize host player - host manages the game, doesn't play
        }
        else
        {
            Debug.LogError("No NetworkScript instance found! Make sure you came from the main menu.");
        }
    }
    
    void Update()
    {
        // Check for player list updates
        if (NetworkScript.Instance != null)
        {
            var currentPlayers = NetworkScript.Instance.playersInLobby;
            if (HasPlayerListChanged(currentPlayers))
            {
                lastPlayerList = new List<string>(currentPlayers);
                UpdatePlayerDataFromNetworkScript();
            }
        }
    }
    
    // --- Player Data Management ---
    
    private void UpdatePlayerDataFromNetworkScript()
    {
        if (NetworkScript.Instance == null) return;
        
        var serverPlayers = NetworkScript.Instance.playersInLobby;
        
        // Instead of clearing all data, update existing players and add new ones
        List<string> currentPlayerNames = playersData.Select(p => p.playerName).ToList();
        
        // Remove players who are no longer in the lobby
        for (int i = playersData.Count - 1; i >= 0; i--)
        {
            if (!serverPlayers.Contains(playersData[i].playerName))
            {
                // Remove player data
                RemoveDogForPlayer(playersData[i].playerName);
                playersData.RemoveAt(i);
                Debug.Log($"[GameManager] Player left: {playersData[i].playerName}");
            }
        }
        
        // Add new players (preserving health of existing players)
        foreach (string playerName in serverPlayers)
        {
            if (!currentPlayerNames.Contains(playerName))
            {
                PlayerData newPlayer = new PlayerData(playerName, false); // No host players in game
                playersData.Add(newPlayer);
                SpawnDogForPlayer(newPlayer);
                Debug.Log($"[GameManager] Player joined: {playerName}");
            }
        }
        
        Debug.Log($"[GameManager] Player list updated: {serverPlayers.Count} players, {activeDogs.Count} dogs spawned");
    }
    
    private void UpdatePlayerDataFromServer()
    {
        if (NetworkScript.Instance == null) return;
        
        var serverPlayers = NetworkScript.Instance.playersInLobby;
        
        // Add new players
        foreach (string playerName in serverPlayers)
        {
            AddOrUpdatePlayer(playerName, false);
        }
        
        // Remove players who left (keep them for now, just mark as disconnected)
        // You might want to implement a different strategy here
    }
    
    public void AddOrUpdatePlayer(string playerName, bool isHost = false)
    {
        // Check if player already exists
        PlayerData existingPlayer = playersData.Find(p => p.playerName == playerName);
        
        if (existingPlayer == null)
        {
            // Add new player (all players get dogs since host doesn't play)
            PlayerData newPlayer = new PlayerData(playerName, false); // No host players in game
            playersData.Add(newPlayer);
            Debug.Log($"Added player: {playerName} (Health: {newPlayer.health})");
            
            // All players get dogs
            SpawnDogForPlayer(newPlayer);
        }
        else
        {
            // Update existing player
            Debug.Log($"Updated player: {playerName}");
        }
    }
    
    public void ModifyPlayerHealth(string playerName, int healthChange)
    {
        PlayerData player = playersData.Find(p => p.playerName == playerName);
        if (player != null)
        {
            player.health = Mathf.Clamp(player.health + healthChange, 0, 100);
            Debug.Log($"{playerName} health: {player.health}");
        }
    }
    
    public PlayerData GetPlayerData(string playerName)
    {
        return playersData.Find(p => p.playerName == playerName);
    }
    
    public void DamagePlayer(string playerName)
    {
        ModifyPlayerHealth(playerName, -1);
    }
    
    // --- Dog GameObject Management ---
    
    void SpawnDogForPlayer(PlayerData playerData)
    {
        if (dogPrefab == null)
        {
            Debug.LogWarning("Dog prefab not assigned! Please assign a dog prefab in the inspector.");
            return;
        }
        
        // Check if a dog already exists for this player
        GameObject existingDog = GetDogForPlayer(playerData.playerName);
        if (existingDog != null)
        {
            Debug.LogWarning($"Dog already exists for {playerData.playerName}. Skipping spawn.");
            return;
        }
        
        // Get spawn position
        Vector3 spawnPosition = GetNextDogPosition();
        
        // Create dog GameObject
        GameObject newDog = Instantiate(dogPrefab, spawnPosition, Quaternion.identity);
        
        // Set parent for organization
        if (dogContainer != null)
        {
            newDog.transform.SetParent(dogContainer);
        }
        
        // Name the dog GameObject
        newDog.name = $"Dog_{playerData.playerName}";
        
        // Store reference to player data
        DogController dogController = newDog.GetComponent<DogController>();
        if (dogController == null)
        {
            dogController = newDog.AddComponent<DogController>();
        }
        dogController.SetPlayerData(playerData);
        
        // Add to our list
        activeDogs.Add(newDog);
        
        Debug.Log($"Spawned dog for {playerData.playerName} at position {spawnPosition}");
    }
    
    Vector3 GetNextDogPosition()
    {
        // Use predefined positions if available
        if (dogPositions != null && dogPositions.Length > 0)
        {
            int index = activeDogs.Count % dogPositions.Length;
            return dogPositions[index];
        }
        
        // Otherwise arrange in a circle
        float angle = activeDogs.Count * (360f / 8f); // Up to 8 dogs in a circle
        float radius = 3f;
        
        return new Vector3(
            Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
            0f,
            Mathf.Sin(angle * Mathf.Deg2Rad) * radius
        );
    }
    
    public GameObject GetDogForPlayer(string playerName)
    {
        return activeDogs.Find(dog => dog.name == $"Dog_{playerName}");
    }
    
    public void RemoveDogForPlayer(string playerName)
    {
        GameObject dogToRemove = GetDogForPlayer(playerName);
        if (dogToRemove != null)
        {
            activeDogs.Remove(dogToRemove);
            Destroy(dogToRemove);
        }
    }
    
    void DisplayLobbyInfo()
    {
        string lobbyCode = NetworkScript.Instance.currentLobbyCode;
        bool isHost = NetworkScript.Instance.isHost;
        
        if (lobbyCodeDisplay != null)
        {
            lobbyCodeDisplay.text = $"Lobby: {lobbyCode}";
        }
        
        Debug.Log($"Game started! Lobby: {lobbyCode}, Host: {isHost}");
    }
    
    // --- Server Game State Management ---
    
    public void UpdateServerGameState(ServerGameState newState, object data = null)
    {
        // Show phase transition for server state changes (except JOINING)
        if (newState != ServerGameState.JOINING && newState != currentServerState)
        {
            StartCoroutine(UpdateServerGameStateWithTransition(newState, data));
        }
        else
        {
            // No transition needed, update immediately
            UpdateServerGameStateImmediate(newState, data);
        }
    }
    
    private IEnumerator UpdateServerGameStateWithTransition(ServerGameState newState, object data)
    {
        // Show phase transition animation for server state
        yield return StartCoroutine(ShowServerStateTransition(newState));
        
        // Update the actual server state after transition
        UpdateServerGameStateImmediate(newState, data);
    }
    
    private void UpdateServerGameStateImmediate(ServerGameState newState, object data)
    {
        currentServerState = newState;
        Debug.Log($"Server game state changed to: {currentServerState}");
        
        // Update UI buttons when server state changes
        UpdatePhaseUI();
        
        // Update timer visibility based on new state
        UpdateTimerVisibility();
        
        // Handle timer data from server
        HandleTimerFromServerData(data);
        
        // Notify host script of state change
        if (hostScript != null)
        {
            Debug.Log($"GameManager calling hostScript.UpdateHostStateForGameState({newState})");
            hostScript.UpdateHostStateForGameState(newState);
        }
        else
        {
            Debug.LogWarning("GameManager: hostScript is null! Please assign HostScript in inspector.");
        }
        
        switch (newState)
        {
            case ServerGameState.PROMPT:
                Debug.Log("Showing prompt waiting UI");
                if (data is GameStateData gameStateData)
                {
                    currentGameStateData = gameStateData;
                    
                    // Sync player data if available
                    if (gameStateData.players != null)
                    {
                        SyncAllPlayerData(gameStateData.players);
                    }
                }
                ShowPromptWaitingUI();
                break;
            case ServerGameState.GENERATING:
                Debug.Log("Showing loading UI");
                ShowLoadingUI();
                // Stop timer during generation phase
                StopTimer();
                break;
            case ServerGameState.TRIVIA:
                Debug.Log("Received trivia data, showing trivia UI");
                if (data is TriviaData triviaData)
                {
                    currentTriviaData = triviaData;
                    Debug.Log($"Trivia data: Question='{triviaData.question}', Options count={triviaData.options?.Length ?? 0}");
                    
                    // Sync player data if available
                    if (triviaData.players != null)
                    {
                        SyncAllPlayerData(triviaData.players);
                    }
                    
                    ShowTriviaUI();
                }
                else
                {
                    Debug.LogError("TRIVIA state received but data is not TriviaData type!");
                }
                break;
            case ServerGameState.REWARD:
                Debug.Log("Processing REWARD state.");
                if (data is RewardData rewardData)
                {
                    currentRewardData = rewardData;
                    
                    // ✅ THE FIX: Immediately sync player health with the authoritative data from the server.
                    // The server has already calculated the damage. We just need to display it.
                    if (rewardData.players != null) 
                    {
                        SyncAllPlayerData(rewardData.players);
                    }

                    ShowRewardUI();
                }
                else
                {
                    Debug.LogError("REWARD state received but data is not RewardData type!");
                }
                break;
            case ServerGameState.ENDGAME:
                Debug.Log("Showing endgame UI");
                if (data is EndGameData endGameData)
                {
                    currentEndGameData = endGameData;
                    
                    // Sync player data if available
                    if (endGameData.players != null)
                    {
                        SyncAllPlayerData(endGameData.players);
                    }
                    
                    ShowEndGameUI();
                }
                else
                {
                    Debug.LogError("ENDGAME state received but data is not EndGameData type!");
                }
                break;
            case ServerGameState.JOINING:
                Debug.Log("Returning to lobby - resetting all UI");
                HideAllGameStatePanels();
                ResetEndGameUI(); // Reset dramatic endgame elements
                // Reset phase display to default
                if (phaseDisplay != null)
                {
                    phaseDisplay.text = $"Phase: {currentPhase}";
                }
                break;
            default:
                Debug.Log("Hiding all game state panels - returning to default state");
                HideAllGameStatePanels();
                ResetEndGameUI(); // Reset dramatic endgame elements in case of unknown state
                // Reset phase display to default
                if (phaseDisplay != null)
                {
                    phaseDisplay.text = $"Phase: {currentPhase}";
                }
                break;
        }
    }
    
    private void HideAllGameStatePanels()
    {
        // Immediately hide all trivia elements first (no fade)
        HideTriviaElementsImmediate();
        
        // Fade out prompt panel if active
        if (promptPanelCanvasGroup != null && promptPanelCanvasGroup.alpha > 0)
        {
            FadeOutPromptPanel();
        }
        else if (promptWaitingPanel != null) 
        {
            bool wasActive = promptWaitingPanel.activeInHierarchy;
            promptWaitingPanel.SetActive(false);
            Debug.Log($"Prompt waiting panel hidden (was active: {wasActive})");
        }
        else
        {
            Debug.LogWarning("Prompt waiting panel is null!");
        }
    }
    
    private void HideTriviaElementsImmediate()
    {
        // Immediately disable trivia elements without fade
        if (questionText != null) 
        {
            questionText.gameObject.SetActive(false);
        }
        if (questionImage != null) 
        {
            questionImage.gameObject.SetActive(false);
        }
        
        if (answerAText != null) 
        {
            answerAText.gameObject.SetActive(false);
        }
        if (answerAImage != null) 
        {
            answerAImage.gameObject.SetActive(false);
        }
        
        if (answerBText != null) 
        {
            answerBText.gameObject.SetActive(false);
        }
        if (answerBImage != null) 
        {
            answerBImage.gameObject.SetActive(false);
        }
        
        if (answerCText != null) 
        {
            answerCText.gameObject.SetActive(false);
        }
        if (answerCImage != null) 
        {
            answerCImage.gameObject.SetActive(false);
        }
        
        if (answerDText != null) 
        {
            answerDText.gameObject.SetActive(false);
        }
        if (answerDImage != null) 
        {
            answerDImage.gameObject.SetActive(false);
        }
        
        // Also disable trivia canvas group if it exists
        if (triviaCanvasGroup != null)
        {
            triviaCanvasGroup.alpha = 0f;
            triviaCanvasGroup.gameObject.SetActive(false);
            triviaCanvasGroup.interactable = false;
        }
        
        Debug.Log("All trivia UI GameObjects immediately disabled");
    }
    
    private void HideTriviaElements()
    {
        // Hide question text and image
        if (questionText != null) 
        {
            questionText.gameObject.SetActive(false);
        }
        if (questionImage != null) 
        {
            questionImage.gameObject.SetActive(false);
        }
        
        // Hide answer texts and images
        if (answerAText != null) 
        {
            answerAText.gameObject.SetActive(false);
        }
        if (answerAImage != null) 
        {
            answerAImage.gameObject.SetActive(false);
        }
        
        if (answerBText != null) 
        {
            answerBText.gameObject.SetActive(false);
        }
        if (answerBImage != null) 
        {
            answerBImage.gameObject.SetActive(false);
        }
        
        if (answerCText != null) 
        {
            answerCText.gameObject.SetActive(false);
        }
        if (answerCImage != null) 
        {
            answerCImage.gameObject.SetActive(false);
        }
        
        if (answerDText != null) 
        {
            answerDText.gameObject.SetActive(false);
        }
        if (answerDImage != null) 
        {
            answerDImage.gameObject.SetActive(false);
        }
        
        Debug.Log("Disabled all trivia UI GameObjects");
    }
    
    private void ShowTriviaElements()
    {
        // Show question text and image
        if (questionText != null) 
        {
            questionText.gameObject.SetActive(true);
        }
        if (questionImage != null) 
        {
            questionImage.gameObject.SetActive(true);
        }
        
        // Show answer texts and images
        if (answerAText != null) 
        {
            answerAText.gameObject.SetActive(true);
        }
        if (answerAImage != null) 
        {
            answerAImage.gameObject.SetActive(true);
        }
        
        if (answerBText != null) 
        {
            answerBText.gameObject.SetActive(true);
        }
        if (answerBImage != null) 
        {
            answerBImage.gameObject.SetActive(true);
        }
        
        if (answerCText != null) 
        {
            answerCText.gameObject.SetActive(true);
        }
        if (answerCImage != null) 
        {
            answerCImage.gameObject.SetActive(true);
        }
        
        if (answerDText != null) 
        {
            answerDText.gameObject.SetActive(true);
        }
        if (answerDImage != null) 
        {
            answerDImage.gameObject.SetActive(true);
        }
        
        Debug.Log("Enabled all trivia UI GameObjects");
    }
    
    // --- Fade Animation Methods ---
    
    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float targetAlpha, float duration, System.Action onComplete = null)
    {
        if (canvasGroup == null)
        {
            Debug.LogWarning("CanvasGroup is null, cannot fade");
            onComplete?.Invoke();
            yield break;
        }
        
        float startAlpha = canvasGroup.alpha;
        float elapsedTime = 0f;
        
        // Enable the canvas group if we're fading in (since initial alpha is 0 in editor)
        if (targetAlpha > 0)
        {
            canvasGroup.gameObject.SetActive(true);
            canvasGroup.interactable = false; // Disable interaction during fade
            Debug.Log($"Fading in CanvasGroup from {startAlpha} to {targetAlpha}");
        }
        else
        {
            Debug.Log($"Fading out CanvasGroup from {startAlpha} to {targetAlpha}");
        }
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            yield return null;
        }
        
        canvasGroup.alpha = targetAlpha;
        
        // Handle completion
        if (targetAlpha <= 0)
        {
            canvasGroup.gameObject.SetActive(false);
            canvasGroup.interactable = false;
        }
        else
        {
            canvasGroup.interactable = true; // Enable interaction after fade in
        }
        
        onComplete?.Invoke();
        
        Debug.Log($"Fade completed - CanvasGroup alpha: {targetAlpha}");
    }
    
    private void StopCurrentFade()
    {
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
            currentFadeCoroutine = null;
        }
    }
    
    private void FadeInPromptPanel()
    {
        StopCurrentFade();
        if (promptPanelCanvasGroup != null)
        {
            // Ensure the GameObject is active first (since initial alpha is 0)
            promptPanelCanvasGroup.gameObject.SetActive(true);
            
            currentFadeCoroutine = StartCoroutine(FadeCanvasGroup(promptPanelCanvasGroup, 1f, fadeInDuration, 
                () => Debug.Log("Prompt panel fade in complete")));
        }
        else
        {
            // Fallback to regular activation
            if (promptWaitingPanel != null)
            {
                promptWaitingPanel.SetActive(true);
                Debug.Log("Prompt panel shown (no CanvasGroup assigned)");
            }
        }
    }
    
    private void FadeOutPromptPanel(System.Action onComplete = null)
    {
        StopCurrentFade();
        if (promptPanelCanvasGroup != null)
        {
            currentFadeCoroutine = StartCoroutine(FadeCanvasGroup(promptPanelCanvasGroup, 0f, fadeOutDuration, 
                () => {
                    Debug.Log("Prompt panel fade out complete");
                    onComplete?.Invoke();
                }));
        }
        else
        {
            // Fallback to regular deactivation
            if (promptWaitingPanel != null)
            {
                promptWaitingPanel.SetActive(false);
                Debug.Log("Prompt panel hidden (no CanvasGroup assigned)");
            }
            onComplete?.Invoke();
        }
    }
    
    private void FadeInTriviaElements()
    {
        StopCurrentFade();
        
        // Show elements first (they start at alpha 0 from editor)
        ShowTriviaElements();
        
        if (triviaCanvasGroup != null)
        {
            // Ensure the GameObject is active and set to alpha 0 (editor setting)
            triviaCanvasGroup.gameObject.SetActive(true);
            triviaCanvasGroup.alpha = 0f; // Respect editor setting
            
            currentFadeCoroutine = StartCoroutine(FadeCanvasGroup(triviaCanvasGroup, 1f, fadeInDuration,
                () => Debug.Log("Trivia elements fade in complete")));
        }
        else
        {
            Debug.Log("Trivia elements shown (no CanvasGroup assigned)");
        }
    }
    
    private void FadeOutTriviaElements(System.Action onComplete = null)
    {
        StopCurrentFade();
        if (triviaCanvasGroup != null)
        {
            currentFadeCoroutine = StartCoroutine(FadeCanvasGroup(triviaCanvasGroup, 0f, fadeOutDuration,
                () => {
                    HideTriviaElements(); // Hide after fade
                    Debug.Log("Trivia elements fade out complete");
                    onComplete?.Invoke();
                }));
        }
        else
        {
            HideTriviaElements();
            Debug.Log("Trivia elements hidden (no CanvasGroup assigned)");
            onComplete?.Invoke();
        }
    }
    
    // --- Phase Transition Methods ---
    
    private IEnumerator ShowServerStateTransition(ServerGameState newState)
    {
        if (phaseTransitionDimImage == null || phaseTransitionText == null)
        {
            Debug.LogWarning("Phase transition components not assigned. Skipping server state transition.");
            yield break;
        }
        
        // Get display name for server state
        string stateDisplayName = GetServerStateDisplayName(newState);
        
        Debug.Log($"[Server State Transition] Starting transition to: {stateDisplayName}");
        
        // Set phase text and enable it (including parent hierarchy)
        phaseTransitionText.text = stateDisplayName;
        EnableUIElementHierarchy(phaseTransitionText.gameObject);
        Debug.Log($"[Server State Transition] Text enabled: {phaseTransitionText.gameObject.activeInHierarchy}");
        
        // Enable overlay and set to transparent
        EnableUIElementHierarchy(phaseTransitionDimImage.gameObject);
        Color imageColor = phaseTransitionDimImage.color;
        imageColor.a = 0f;
        phaseTransitionDimImage.color = imageColor;
        Debug.Log($"[Server State Transition] Dim image enabled: {phaseTransitionDimImage.gameObject.activeInHierarchy}");
        
        // Phase 1: Fade in the overlay (dim screen)
        Debug.Log("[Server State Transition] Phase 1: Fading in overlay");
        yield return StartCoroutine(FadeImage(phaseTransitionDimImage, 0.5f, phaseDimDuration));
        
        // Phase 2: Wait for display duration (showing phase text)
        float remainingDisplayTime = phaseDisplayDuration - phaseDimDuration;
        if (remainingDisplayTime > 0)
        {
            Debug.Log($"[Server State Transition] Phase 2: Displaying text for {remainingDisplayTime} seconds");
            yield return new WaitForSeconds(remainingDisplayTime);
        }
        
        // Phase 3: Fade out the overlay
        Debug.Log("[Server State Transition] Phase 3: Fading out overlay");
        yield return StartCoroutine(FadeImage(phaseTransitionDimImage, 0f, phaseFadeOutDuration));
        
        // Disable both overlay and text
        phaseTransitionDimImage.gameObject.SetActive(false);
        phaseTransitionText.gameObject.SetActive(false);
        
        Debug.Log($"Server state transition completed for: {stateDisplayName}");
    }
    
    private string GetServerStateDisplayName(ServerGameState state)
    {
        switch (state)
        {
            case ServerGameState.JOINING:
                return "Waiting for Players";
            case ServerGameState.PROMPT:
                return "Choose Category";
            case ServerGameState.GENERATING:
                return "Generating Questions";
            case ServerGameState.TRIVIA:
                return "Trivia Time!";
            case ServerGameState.REWARD:
                return "WHO GOT IT RIGHT?";
            case ServerGameState.ENDGAME:
                return "Game Over";
            default:
                return state.ToString();
        }
    }
    
    private IEnumerator FadeImage(Image image, float targetAlpha, float duration)
    {
        if (image == null)
        {
            yield break;
        }
        
        Color startColor = image.color;
        float startAlpha = startColor.a;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            
            Color newColor = startColor;
            newColor.a = currentAlpha;
            image.color = newColor;
            
            yield return null;
        }
        
        // Ensure final alpha is set
        Color finalColor = startColor;
        finalColor.a = targetAlpha;
        image.color = finalColor;
    }
    
    private IEnumerator ShowPhaseTransition(GamePhase newPhase)
    {
        if (phaseTransitionDimImage == null || phaseTransitionText == null)
        {
            Debug.LogWarning("Phase transition components not assigned. Skipping transition animation.");
            yield break;
        }
        
        Debug.Log($"[Phase Transition] Starting transition to: {newPhase}");
        
        // Set phase text and enable it (including parent hierarchy)
        string phaseDisplayName = GetPhaseDisplayName(newPhase);
        phaseTransitionText.text = phaseDisplayName;
        
        // Enable the text component and ensure parent hierarchy is active
        EnableUIElementHierarchy(phaseTransitionText.gameObject);
        Debug.Log($"[Phase Transition] Text enabled: {phaseTransitionText.gameObject.activeInHierarchy}");
        
        // Enable overlay and set to transparent
        EnableUIElementHierarchy(phaseTransitionDimImage.gameObject);
        Color imageColor = phaseTransitionDimImage.color;
        imageColor.a = 0f;
        phaseTransitionDimImage.color = imageColor;
        Debug.Log($"[Phase Transition] Dim image enabled: {phaseTransitionDimImage.gameObject.activeInHierarchy}");
        
        Debug.Log($"Starting phase transition to: {phaseDisplayName}");
        
        // Phase 1: Fade in the overlay (dim screen)
        Debug.Log("[Phase Transition] Phase 1: Fading in overlay");
        yield return StartCoroutine(FadeImage(phaseTransitionDimImage, 0.5f, phaseDimDuration));
        
        // Phase 2: Wait for display duration (showing phase text)
        float remainingDisplayTime = phaseDisplayDuration - phaseDimDuration;
        if (remainingDisplayTime > 0)
        {
            Debug.Log($"[Phase Transition] Phase 2: Displaying text for {remainingDisplayTime} seconds");
            yield return new WaitForSeconds(remainingDisplayTime);
        }
        
        // Phase 3: Fade out the overlay
        Debug.Log("[Phase Transition] Phase 3: Fading out overlay");
        yield return StartCoroutine(FadeImage(phaseTransitionDimImage, 0f, phaseFadeOutDuration));
        
        // Disable both overlay and text
        phaseTransitionDimImage.gameObject.SetActive(false);
        phaseTransitionText.gameObject.SetActive(false);
        
        Debug.Log($"Phase transition completed for: {phaseDisplayName}");
    }
    
    private void EnableUIElementHierarchy(GameObject uiElement)
    {
        if (uiElement == null) return;
        
        // Enable the element itself
        uiElement.SetActive(true);
        
        // Walk up the parent hierarchy and enable any disabled parents
        Transform parent = uiElement.transform.parent;
        while (parent != null)
        {
            if (!parent.gameObject.activeInHierarchy)
            {
                Debug.Log($"[Phase Transition] Enabling parent: {parent.name}");
                parent.gameObject.SetActive(true);
            }
            parent = parent.parent;
        }
    }
    
    // --- Dynamic Font Sizing Methods ---
    
    private void SetupTextAutoSizing(TextMeshProUGUI textComponent, float maxSize, bool isQuestion = false)
    {
        if (textComponent == null || !enableAutoSizing) return;
        
        // Enable TextMeshPro auto-sizing
        textComponent.enableAutoSizing = true;
        textComponent.fontSizeMin = minFontSize;
        textComponent.fontSizeMax = maxSize;
        
        // Set text wrapping and overflow settings
        textComponent.textWrappingMode = TextWrappingModes.Normal;
        textComponent.overflowMode = TextOverflowModes.Truncate;
        
        Debug.Log($"Setup auto-sizing for {textComponent.name}: min={minFontSize}, max={maxSize}");
    }
    
    private void AdjustTextToFit(TextMeshProUGUI textComponent, string text, float maxSize, bool isQuestion = false)
    {
        if (textComponent == null || !enableAutoSizing) 
        {
            if (textComponent != null) textComponent.text = text;
            return;
        }
        
        // Set the text first
        textComponent.text = text;
        
        // Setup auto-sizing
        SetupTextAutoSizing(textComponent, maxSize, isQuestion);
        
        // Force the text to update
        textComponent.ForceMeshUpdate();
        
        // Check if text is still overflowing and adjust if needed
        StartCoroutine(AdjustTextAfterFrame(textComponent, text, maxSize, isQuestion));
    }
    
    private IEnumerator AdjustTextAfterFrame(TextMeshProUGUI textComponent, string text, float maxSize, bool isQuestion)
    {
        // Wait a frame for layout to update
        yield return null;
        
        if (textComponent == null) yield break;
        
        // Force mesh update to get accurate bounds
        textComponent.ForceMeshUpdate();
        
        // Get the text bounds
        Vector3[] textCorners = new Vector3[4];
        textComponent.rectTransform.GetWorldCorners(textCorners);
        
        // Check if we need to truncate text for extreme cases
        if (textComponent.isTextOverflowing)
        {
            Debug.LogWarning($"Text still overflowing in {textComponent.name}, attempting truncation");
            TruncateTextToFit(textComponent, text, maxSize);
        }
        
        Debug.Log($"Final font size for {textComponent.name}: {textComponent.fontSize}");
    }
    
    private void TruncateTextToFit(TextMeshProUGUI textComponent, string originalText, float maxSize)
    {
        if (textComponent == null) return;
        
        string truncatedText = originalText;
        int maxAttempts = 10;
        int attempts = 0;
        
        while (textComponent.isTextOverflowing && attempts < maxAttempts && truncatedText.Length > 10)
        {
            // Remove 10% of the text and add ellipsis
            int removeLength = Mathf.Max(1, truncatedText.Length / 10);
            truncatedText = truncatedText.Substring(0, truncatedText.Length - removeLength) + "...";
            
            textComponent.text = truncatedText;
            textComponent.ForceMeshUpdate();
            attempts++;
        }
        
        if (attempts > 0)
        {
            Debug.Log($"Truncated text for {textComponent.name} after {attempts} attempts");
        }
    }
    
    private void SetupAllTextComponents()
    {
        // Setup auto-sizing for all text components
        if (questionText != null)
        {
            SetupTextAutoSizing(questionText, maxFontSize, true);
        }
        
        if (answerAText != null)
        {
            SetupTextAutoSizing(answerAText, maxAnswerFontSize, false);
        }
        
        if (answerBText != null)
        {
            SetupTextAutoSizing(answerBText, maxAnswerFontSize, false);
        }
        
        if (answerCText != null)
        {
            SetupTextAutoSizing(answerCText, maxAnswerFontSize, false);
        }
        
        if (answerDText != null)
        {
            SetupTextAutoSizing(answerDText, maxAnswerFontSize, false);
        }
        
        Debug.Log("Auto-sizing setup complete for all text components");
    }
    
    private void ShowPromptWaitingUI()
    {
        // Immediately hide trivia elements first
        HideTriviaElementsImmediate();
        HideAllGameStatePanels(); // Hide everything first
        
        // Small delay to let fade out complete, then fade in
        StartCoroutine(DelayedFadeInPrompt());
    }
    
    private IEnumerator DelayedFadeInPrompt()
    {
        yield return new WaitForSeconds(fadeOutDuration + 0.1f); // Wait for fade out + small buffer
        FadeInPromptPanel();
    }
    
    private void ShowLoadingUI()
    {
        // Immediately hide all UI elements including trivia
        HideTriviaElementsImmediate();
        HideAllGameStatePanels();
        
        // Double-check prompt panel is hidden
        if (promptWaitingPanel != null && promptWaitingPanel.activeInHierarchy)
        {
            promptWaitingPanel.SetActive(false);
            Debug.Log("Force hiding prompt panel in loading state");
        }
        
        // Update phase display after a short delay
        StartCoroutine(DelayedShowLoading());
    }
    
    private IEnumerator DelayedShowLoading()
    {
        yield return new WaitForSeconds(fadeOutDuration + 0.1f);
        
        // Just use the phase display for loading state
        if (phaseDisplay != null)
        {
            phaseDisplay.text = "Generating Questions...";
            Debug.Log("Loading state - updated phase display, prompt panel hidden");
        }
        else
        {
            Debug.LogWarning("Phase display not assigned!");
        }
    }
    
    private void ShowTriviaUI()
    {
        HideAllGameStatePanels(); // This hides prompt panel and trivia elements
        
        // Double-check prompt panel is hidden
        if (promptWaitingPanel != null && promptWaitingPanel.activeInHierarchy)
        {
            promptWaitingPanel.SetActive(false);
            Debug.Log("Force hiding prompt panel in trivia state");
        }
        
        // Update phase display
        if (phaseDisplay != null)
        {
            phaseDisplay.text = "Trivia Question";
        }
        
        // Show trivia with fade after other elements are hidden
        StartCoroutine(DelayedShowTrivia());
    }
    
    private IEnumerator DelayedShowTrivia()
    {
        yield return new WaitForSeconds(fadeOutDuration + 0.1f); // Wait for fade out
        
        if (currentTriviaData != null)
        {
            // Set the question and answer texts first (invisible)
            SetTriviaContent();
            
            // Then fade in the trivia elements
            FadeInTriviaElements();
        }
        else
        {
            Debug.LogError("Current trivia data is null!");
        }
    }
    
    private void SetTriviaContent()
    {
        // Display question and answers with validation and dynamic sizing
        if (questionText != null)
        {
            if (!string.IsNullOrEmpty(currentTriviaData.question))
            {
                AdjustTextToFit(questionText, currentTriviaData.question, maxFontSize, true);
                Debug.Log($"Set question text with auto-sizing: {currentTriviaData.question}");
            }
            else
            {
                AdjustTextToFit(questionText, "Question not available", maxFontSize, true);
                Debug.LogWarning("Trivia question is empty or null");
            }
        }
        else
        {
            Debug.LogWarning("Question text component not assigned!");
        }
        
        // Set answer options with validation and dynamic sizing
        if (currentTriviaData.options != null && currentTriviaData.options.Length >= 4)
        {
            if (answerAText != null)
            {
                string answerA = $"A. {currentTriviaData.options[0]}";
                AdjustTextToFit(answerAText, answerA, maxAnswerFontSize, false);
                Debug.Log($"Set answer A with auto-sizing: {answerA}");
            }
            else
            {
                Debug.LogWarning("Answer A text component not assigned!");
            }
            
            if (answerBText != null)
            {
                string answerB = $"B. {currentTriviaData.options[1]}";
                AdjustTextToFit(answerBText, answerB, maxAnswerFontSize, false);
                Debug.Log($"Set answer B with auto-sizing: {answerB}");
            }
            else
            {
                Debug.LogWarning("Answer B text component not assigned!");
            }
            
            if (answerCText != null)
            {
                string answerC = $"C. {currentTriviaData.options[2]}";
                AdjustTextToFit(answerCText, answerC, maxAnswerFontSize, false);
                Debug.Log($"Set answer C with auto-sizing: {answerC}");
            }
            else
            {
                Debug.LogWarning("Answer C text component not assigned!");
            }
            
            if (answerDText != null)
            {
                string answerD = $"D. {currentTriviaData.options[3]}";
                AdjustTextToFit(answerDText, answerD, maxAnswerFontSize, false);
                Debug.Log($"Set answer D with auto-sizing: {answerD}");
            }
            else
            {
                Debug.LogWarning("Answer D text component not assigned!");
            }
        }
        else
        {
            Debug.LogError("Trivia options are missing or incomplete!");
            // Set default error messages with auto-sizing
            if (answerAText != null) AdjustTextToFit(answerAText, "A. Option not available", maxAnswerFontSize, false);
            if (answerBText != null) AdjustTextToFit(answerBText, "B. Option not available", maxAnswerFontSize, false);
            if (answerCText != null) AdjustTextToFit(answerCText, "C. Option not available", maxAnswerFontSize, false);
            if (answerDText != null) AdjustTextToFit(answerDText, "D. Option not available", maxAnswerFontSize, false);
        }
        
        Debug.Log("Trivia content set with dynamic font sizing, ready for fade in");
    }
    
    private void ShowRewardUI()
    {
        // Immediately hide all trivia elements
        HideTriviaElementsImmediate();
        HideAllGameStatePanels(); // This hides prompt panel and trivia elements
        
        // Update phase display
        if (phaseDisplay != null)
        {
            phaseDisplay.text = "Processing Results...";
        }
        
        Debug.Log("Reward UI shown - prompt panel hidden, trivia elements immediately hidden");
        
        if (currentRewardData != null)
        {
            // ✅ REMOVED: UpdatePlayerHealthFromRewards() - health is now synced in UpdateServerGameState
            // The server has already sent us the authoritative player health data
            
            // Show animations for all players - stars for correct, blood for incorrect
            StartRewardAnimations();
        }
    }
    
    private void ShowEndGameUI()
    {
        Debug.Log("[EndGame] Starting dramatic endgame sequence");
        
        // Immediately hide all trivia elements and UI panels
        HideTriviaElementsImmediate();
        HideAllGameStatePanels();
        
        // Hide the phase display and other UI elements during endgame
        if (phaseDisplay != null)
            phaseDisplay.gameObject.SetActive(false);
        if (lobbyCodeDisplay != null)
            lobbyCodeDisplay.gameObject.SetActive(false);
        
        // Start the dramatic endgame sequence
        StartCoroutine(ShowDramaticEndGame());
        
        Debug.Log("[EndGame] Dramatic endgame sequence initiated");
    }
    
    private IEnumerator ShowDramaticEndGame()
    {
        // Step 1: Ensure the overlay and winner text are ready but hidden
        if (endGameDarkOverlay != null)
        {
            endGameDarkOverlay.gameObject.SetActive(true);
            Color overlayColor = endGameOverlayColor;
            overlayColor.a = 0f;
            endGameDarkOverlay.color = overlayColor;
        }
        
        if (winnerAnnouncementText != null)
        {
            winnerAnnouncementText.gameObject.SetActive(true);
            winnerAnnouncementText.color = new Color(winnerAnnouncementText.color.r, 
                                                   winnerAnnouncementText.color.g, 
                                                   winnerAnnouncementText.color.b, 0f);
        }
        
        if (endGameCanvasGroup != null)
        {
            endGameCanvasGroup.alpha = 0f;
            endGameCanvasGroup.gameObject.SetActive(true);
        }
        
        // Step 2: Fade in the dark overlay (everything goes dark except dogs)
        Debug.Log("[EndGame] Fading in dark overlay");
        yield return StartCoroutine(FadeOverlayToTarget(1f, endGameFadeDuration * 0.6f));
        
        // Step 3: Wait a moment for dramatic effect, then move winning dog to center
        yield return new WaitForSeconds(dogMoveDelay);
        
        // Step 3.5: Move the winning dog to center of screen
        Debug.Log("[EndGame] Moving winning dog to center");
        StartCoroutine(MoveWinningDogToCenter());
        
        // Step 4: Fade in the "WE HAVE A WINNER" text
        Debug.Log("[EndGame] Showing winner announcement");
        yield return StartCoroutine(FadeWinnerText(1f, endGameFadeDuration * 0.4f));
        
        // Step 5: Update winner text based on game data
        UpdateWinnerText();
        
        // Step 6: Make dogs stand out (they should already be visible due to sorting layers)
        HighlightWinningDogs();
        
        // Step 7: Show main menu button after a delay
        yield return new WaitForSeconds(1.5f);
        ShowMainMenuButton();
        
        Debug.Log("[EndGame] Dramatic endgame sequence complete");
    }
    
    private IEnumerator FadeOverlayToTarget(float targetAlpha, float duration)
    {
        if (endGameDarkOverlay == null) yield break;
        
        Color startColor = endGameDarkOverlay.color;
        Color targetColor = endGameOverlayColor;
        targetColor.a = targetAlpha;
        
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            endGameDarkOverlay.color = Color.Lerp(startColor, targetColor, progress);
            yield return null;
        }
        
        endGameDarkOverlay.color = targetColor;
    }
    
    private IEnumerator FadeWinnerText(float targetAlpha, float duration)
    {
        if (winnerAnnouncementText == null) yield break;
        
        Color startColor = winnerAnnouncementText.color;
        Color targetColor = startColor;
        targetColor.a = targetAlpha;
        
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            winnerAnnouncementText.color = Color.Lerp(startColor, targetColor, progress);
            yield return null;
        }
        
        winnerAnnouncementText.color = targetColor;
    }
    
    private void UpdateWinnerText()
    {
        if (winnerAnnouncementText == null) return;
        
        if (currentEndGameData?.winners != null && currentEndGameData.winners.Length > 0)
        {
            if (currentEndGameData.winners.Length == 1)
            {
                winnerAnnouncementText.text = $"WE HAVE A WINNER!\n{currentEndGameData.winners[0]}";
            }
            else
            {
                winnerAnnouncementText.text = $"WE HAVE WINNERS!\n{string.Join(" & ", currentEndGameData.winners)}";
            }
        }
        else
        {
            winnerAnnouncementText.text = "GAME OVER\nNo Winners This Round";
        }
        
        Debug.Log($"[EndGame] Winner text updated: {winnerAnnouncementText.text}");
    }
    
    private void HighlightWinningDogs()
    {
        if (currentEndGameData?.winners == null) return;
        
        Debug.Log($"[EndGame] Highlighting {currentEndGameData.winners.Length} winning dogs");
        
        // Highlight winning dogs with magical outline
        foreach (string winnerName in currentEndGameData.winners)
        {
            GameObject winnerDog = GetDogForPlayer(winnerName);
            if (winnerDog != null)
            {
                DogController dogController = winnerDog.GetComponent<DogController>();
                if (dogController != null)
                {
                    dogController.ApplyMagicalOutline(); // Highlight the winner
                    Debug.Log($"[EndGame] Highlighted winning dog: {winnerName}");
                }
            }
            else
            {
                Debug.LogWarning($"[EndGame] Could not find dog for winner: {winnerName}");
            }
        }
    }
    
    private IEnumerator MoveWinningDogToCenter()
    {
        Debug.Log($"[EndGame] MoveWinningDogToCenter started");
        
        if (currentEndGameData?.winners == null || currentEndGameData.winners.Length == 0)
        {
            Debug.LogWarning("[EndGame] No winners to move to center");
            yield break;
        }
        
        // For multiple winners, just move the first one for dramatic effect
        string winnerName = currentEndGameData.winners[0];
        Debug.Log($"[EndGame] Looking for dog for winner: {winnerName}");
        
        GameObject winnerDog = GetDogForPlayer(winnerName);
        
        if (winnerDog == null)
        {
            Debug.LogWarning($"[EndGame] Could not find dog for winner: {winnerName}");
            yield break;
        }
        
        Debug.Log($"[EndGame] Found winner dog: {winnerDog.name}");
        
        Vector3 startPosition = winnerDog.transform.position;
        Vector3 targetPosition = centerScreenPosition;
        
        Debug.Log($"[EndGame] DEBUGGING VALUES:");
        Debug.Log($"[EndGame] centerScreenPosition field value: {centerScreenPosition}");
        Debug.Log($"[EndGame] targetPosition variable value: {targetPosition}");
        Debug.Log($"[EndGame] startPosition: {startPosition}");
        Debug.Log($"[EndGame] Moving {winnerName}'s dog from {startPosition} to {targetPosition}");
        
        float elapsedTime = 0f;
        while (elapsedTime < dogMoveDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / dogMoveDuration;
            
            // Use smooth easing for more dramatic movement
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            
            Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, easedProgress);
            winnerDog.transform.position = newPosition;
            
            // Debug every few frames
            if (Time.frameCount % 10 == 0)
            {
                Debug.Log($"[EndGame] Moving progress: {progress:F2}, position: {newPosition}");
            }
            
            yield return null;
        }
        
        winnerDog.transform.position = targetPosition;
        Debug.Log($"[EndGame] {winnerName}'s dog reached final position: {winnerDog.transform.position}");
        
        // FORCE SET THE POSITION MULTIPLE TIMES TO OVERRIDE ANY INTERFERENCE
        for (int i = 0; i < 5; i++)
        {
            winnerDog.transform.position = new Vector3(-3.5f, -1.5f, 0f);
            yield return new WaitForSeconds(0.02f);
            Debug.Log($"[EndGame] Force set attempt {i + 1}: {winnerDog.transform.position}");
        }
        
        // Wait a moment and check if position changed
        yield return new WaitForSeconds(0.1f);
        Debug.Log($"[EndGame] Position after 0.1s delay: {winnerDog.transform.position}");
    }
    
    private void ShowMainMenuButton()
    {
        if (mainMenuButton != null)
        {
            mainMenuButton.gameObject.SetActive(true);
            Debug.Log("[EndGame] Main menu button shown");
        }
        else
        {
            Debug.LogWarning("[EndGame] Main menu button not assigned");
        }
    }
    
    public void ResetEndGameUI()
    {
        Debug.Log("[EndGame] Resetting endgame UI elements");
        
        // Hide and reset the endgame overlay
        if (endGameDarkOverlay != null)
        {
            endGameDarkOverlay.gameObject.SetActive(false);
            Color overlayColor = endGameOverlayColor;
            overlayColor.a = 0f;
            endGameDarkOverlay.color = overlayColor;
        }
        
        // Hide and reset the winner announcement text
        if (winnerAnnouncementText != null)
        {
            winnerAnnouncementText.gameObject.SetActive(false);
            winnerAnnouncementText.color = new Color(winnerAnnouncementText.color.r, 
                                                   winnerAnnouncementText.color.g, 
                                                   winnerAnnouncementText.color.b, 0f);
        }
        
        // Hide the main menu button
        if (mainMenuButton != null)
        {
            mainMenuButton.gameObject.SetActive(false);
        }
        
        // Reset endgame canvas group
        if (endGameCanvasGroup != null)
        {
            endGameCanvasGroup.alpha = 0f;
            endGameCanvasGroup.gameObject.SetActive(false);
        }
        
        // Show normal UI elements again
        if (phaseDisplay != null)
            phaseDisplay.gameObject.SetActive(true);
        if (lobbyCodeDisplay != null)
            lobbyCodeDisplay.gameObject.SetActive(true);
    }
    
    // --- Timer Visibility Management ---
    
    private void UpdateTimerVisibility()
    {
        // Show timer icon and display for all phases except ENDGAME
        bool shouldShowTimer = currentServerState != ServerGameState.ENDGAME;
        
        if (timerIcon != null)
        {
            timerIcon.gameObject.SetActive(shouldShowTimer);
        }
        
        if (roundTimerDisplay != null)
        {
            roundTimerDisplay.gameObject.SetActive(shouldShowTimer);
        }
        
        Debug.Log($"[Timer] Timer visibility updated - showing: {shouldShowTimer} for state: {currentServerState}");
    }
    
    // --- Player Health and Damage Management ---
    
    // ✅ NEW: More robust function to sync all player data, including health.
    // This method trusts the server's authoritative state completely.
    public void SyncAllPlayerData(PlayerData[] serverPlayers)
    {
        if (serverPlayers == null) 
        {
            Debug.LogWarning("[SyncAllPlayerData] Server players array is null");
            return;
        }

        Debug.Log($"[SyncAllPlayerData] Syncing data for {serverPlayers.Length} players from server");
        
        foreach (var serverPlayer in serverPlayers)
        {
            // Find the corresponding local player data
            PlayerData localPlayer = playersData.Find(p => p.playerName == serverPlayer.playerName);
            if (localPlayer != null)
            {
                // Log health changes for debugging
                int oldHealth = localPlayer.health;
                bool oldSubmitted = localPlayer.submitted;
                
                // Update all fields from the server's authoritative state
                localPlayer.health = serverPlayer.health;
                localPlayer.submitted = serverPlayer.submitted;
                localPlayer.isHost = serverPlayer.isHost;
                
                // Log significant changes
                if (oldHealth != serverPlayer.health)
                {
                    Debug.Log($"[SyncAllPlayerData] {serverPlayer.playerName}: Health {oldHealth} -> {serverPlayer.health}");
                }
                if (oldSubmitted != serverPlayer.submitted)
                {
                    Debug.Log($"[SyncAllPlayerData] {serverPlayer.playerName}: Submitted {oldSubmitted} -> {serverPlayer.submitted}");
                }
                
                // Update the dog's player data reference to trigger health display update
                GameObject playerDog = GetDogForPlayer(serverPlayer.playerName);
                if (playerDog != null)
                {
                    DogController dogController = playerDog.GetComponent<DogController>();
                    if (dogController != null)
                    {
                        dogController.SetPlayerData(localPlayer);
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[SyncAllPlayerData] Player not found locally: {serverPlayer.playerName}");
                // Optionally, you could add the player here if they don't exist locally
            }
        }
        
        Debug.Log("[SyncAllPlayerData] Player data sync completed");
    }
    
    // Original sync method - kept for backward compatibility with other states
    private void SyncPlayerDataFromServer(PlayerData[] serverPlayers)
    {
        if (serverPlayers == null) return;
        
        foreach (var serverPlayer in serverPlayers)
        {
            PlayerData localPlayer = playersData.Find(p => p.playerName == serverPlayer.playerName);
            if (localPlayer != null)
            {
                // Update all player data to match server
                int oldHealth = localPlayer.health;
                localPlayer.health = serverPlayer.health;
                localPlayer.submitted = serverPlayer.submitted;
                localPlayer.isHost = serverPlayer.isHost;
                
                if (oldHealth != serverPlayer.health)
                {
                    Debug.Log($"[Health Sync] {serverPlayer.playerName}: {oldHealth} -> {serverPlayer.health}");
                }
                
                // Update the dog's player data reference to trigger health display update
                GameObject playerDog = GetDogForPlayer(serverPlayer.playerName);
                if (playerDog != null)
                {
                    DogController dogController = playerDog.GetComponent<DogController>();
                    if (dogController != null)
                    {
                        dogController.SetPlayerData(localPlayer);
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[Health Sync] Player not found locally: {serverPlayer.playerName}");
            }
        }
    }
    
    // ✅ REMOVED: UpdatePlayerHealthFromRewards() method
    // Health syncing is now handled by SyncAllPlayerData() called from UpdateServerGameState()
    
    private void StartRewardAnimations()
    {
        if (currentRewardData?.results == null) 
        {
            Debug.LogWarning("[StartRewardAnimations] No results data available");
            return;
        }

        Debug.Log($"[StartRewardAnimations] Processing {currentRewardData.results.Length} player results");
        
        foreach (var result in currentRewardData.results)
        {
            Debug.Log($"[StartRewardAnimations] Player {result.playerId}: isCorrect={result.isCorrect}");
            
            // Use the playerName from the result if available, otherwise try to convert ID to name
            string playerName = !string.IsNullOrEmpty(result.playerName) ? result.playerName : GetPlayerNameFromId(result.playerId);
            if (string.IsNullOrEmpty(playerName))
            {
                Debug.LogWarning($"[StartRewardAnimations] Could not find player name for ID: {result.playerId}");
                continue;
            }
            
            // Find the dog for this player using the player name
            GameObject playerDog = GetDogForPlayer(playerName);
            if (playerDog != null)
            {
                DogController dogController = playerDog.GetComponent<DogController>();
                if (dogController != null)
                {
                    if (result.isCorrect)
                    {
                        // Correct answer - show star effect
                        Debug.Log($"[StartRewardAnimations] Triggering star effect for {playerName} (ID: {result.playerId})");
                        StartCoroutine(SpawnStarEffectForDog(playerDog));
                    }
                    else
                    {
                        // Incorrect answer - show blood/damage effect
                        Debug.Log($"[StartRewardAnimations] Triggering blood effect for {playerName} (ID: {result.playerId})");
                        dogController.TriggerDamageEffect();
                        StartCoroutine(SpawnBloodEffectForDog(playerDog));
                    }
                }
                else
                {
                    Debug.LogWarning($"[StartRewardAnimations] No DogController found for {playerName}");
                }
            }
            else
            {
                Debug.LogWarning($"[StartRewardAnimations] No dog found for player {playerName} (ID: {result.playerId})");
            }
        }
    }
    
    private IEnumerator SpawnStarEffectForDog(GameObject dogObject)
    {
        if (dogObject == null) yield break;
        
        Vector3 dogPosition = dogObject.transform.position;
        Vector3 effectPosition = dogPosition + effectOffset;
        
        if (starEffectPrefab != null)
        {
            // Instantiate your prepared star effect prefab
            Debug.Log($"[SpawnStarEffectForDog] Spawning star effect at {effectPosition}");
            GameObject starEffect = Instantiate(starEffectPrefab, effectPosition, Quaternion.identity);
            
            // Optionally destroy the effect after a certain duration
            if (effectDuration > 0)
            {
                Destroy(starEffect, effectDuration);
            }
        }
        else
        {
            Debug.LogWarning("[SpawnStarEffectForDog] Star effect prefab not assigned! Assign starEffectPrefab in the inspector.");
            // Fallback: create a simple golden glow effect
            yield return StartCoroutine(SimpleGoldenGlowEffect(dogObject));
        }
    }
    
    private IEnumerator SpawnBloodEffectForDog(GameObject dogObject)
    {
        if (dogObject == null) yield break;
        
        Vector3 dogPosition = dogObject.transform.position;
        Vector3 effectPosition = dogPosition + effectOffset;
        
        if (bloodEffectPrefab != null)
        {
            // Instantiate your prepared blood effect prefab
            Debug.Log($"[SpawnBloodEffectForDog] Spawning blood effect at {effectPosition}");
            GameObject bloodEffect = Instantiate(bloodEffectPrefab, effectPosition, Quaternion.identity);
            
            // Optionally destroy the effect after a certain duration
            if (effectDuration > 0)
            {
                Destroy(bloodEffect, effectDuration);
            }
        }
        else
        {
            Debug.LogWarning("[SpawnBloodEffectForDog] Blood effect prefab not assigned! Assign bloodEffectPrefab in the inspector.");
            // Fallback: create simple blood particle effect
            yield return StartCoroutine(CreateSimpleBloodEffect(dogObject));
        }
    }
    
    private IEnumerator CreateSimpleBloodEffect(GameObject dogObject)
    {
        if (dogObject == null) yield break;
        
        Vector3 dogPosition = dogObject.transform.position;
        
        // Create blood particle effect
        int numberOfParticles = 12;
        
        for (int i = 0; i < numberOfParticles; i++)
        {
            // Create a simple red particle
            GameObject bloodParticle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bloodParticle.name = "BloodParticle";
            
            // Remove collider since we don't need physics
            Collider collider = bloodParticle.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            
            // Set position near the dog
            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-0.5f, 0.5f),
                UnityEngine.Random.Range(-0.5f, 0.5f),
                0f
            );
            bloodParticle.transform.position = dogPosition + randomOffset;
            bloodParticle.transform.localScale = Vector3.one * 0.1f;
            
            // Make it red
            Renderer renderer = bloodParticle.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.8f, 0.1f, 0.1f, 1f); // Dark red
            }
            
            // Animate the particle
            StartCoroutine(AnimateBloodParticle(bloodParticle));
            
            // Small delay between particles
            yield return new WaitForSeconds(0.02f);
        }
    }
    
    private IEnumerator AnimateBloodParticle(GameObject particle)
    {
        if (particle == null) yield break;
        
        Vector3 startPosition = particle.transform.position;
        Vector3 velocity = new Vector3(
            UnityEngine.Random.Range(-2f, 2f),
            UnityEngine.Random.Range(1f, 3f),
            0f
        );
        
        float gravity = -5f;
        float lifetime = 1.5f;
        float elapsed = 0f;
        
        Renderer renderer = particle.GetComponent<Renderer>();
        Color startColor = renderer.material.color;
        
        while (elapsed < lifetime && particle != null)
        {
            elapsed += Time.deltaTime;
            
            // Physics simulation
            velocity.y += gravity * Time.deltaTime;
            particle.transform.position += velocity * Time.deltaTime;
            
            // Fade out
            float alpha = 1f - (elapsed / lifetime);
            if (renderer != null)
            {
                renderer.material.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }
            
            yield return null;
        }
        
        // Clean up
        if (particle != null)
        {
            Destroy(particle);
        }
    }
    
    private IEnumerator SimpleGoldenGlowEffect(GameObject dogObject)
    {
        if (dogObject == null) yield break;
        
        // Find the dog's renderer components
        SpriteRenderer[] renderers = dogObject.GetComponentsInChildren<SpriteRenderer>();
        if (renderers.Length == 0) yield break;
        
        // Store original colors
        Color[] originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].color;
        }
        
        // Golden glow effect
        Color goldenColor = new Color(1f, 0.8f, 0.2f, 1f); // Golden yellow
        float duration = 2f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float intensity = Mathf.Sin((elapsed / duration) * Mathf.PI * 3f) * 0.5f + 0.5f;
            
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].color = Color.Lerp(originalColors[i], goldenColor, intensity * 0.6f);
                }
            }
            
            yield return null;
        }
        
        // Restore original colors
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].color = originalColors[i];
            }
        }
    }
    
    private string GetPlayerNameFromId(string playerId)
    {
        // For now, we'll use the playerName from the result instead
        // This method is a fallback - we should use result.playerName directly
        Debug.LogWarning($"[GetPlayerNameFromId] Using fallback method for ID: {playerId}");
        return null;
    }
    
    public void UpdatePlayerSubmissionStatus(string playerName, bool submitted)
    {
        PlayerData player = playersData.Find(p => p.playerName == playerName);
        if (player != null)
        {
            player.submitted = submitted;
        }
    }
    
    // Comprehensive player data update method
    public void UpdatePlayerData(string playerName, int? health = null, bool? submitted = null, bool? isHost = null)
    {
        PlayerData player = playersData.Find(p => p.playerName == playerName);
        if (player != null)
        {
            int oldHealth = player.health;
            
            // Update fields if provided
            if (health.HasValue) player.health = health.Value;
            if (submitted.HasValue) player.submitted = submitted.Value;
            if (isHost.HasValue) player.isHost = isHost.Value;
            
            // Log health changes
            if (health.HasValue && oldHealth != health.Value)
            {
                Debug.Log($"[Player Update] {playerName}: Health {oldHealth} -> {health.Value}");
            }
            
            // Update the dog's health display if health changed
            if (health.HasValue && oldHealth != health.Value)
            {
                GameObject playerDog = GetDogForPlayer(playerName);
                if (playerDog != null)
                {
                    DogController dogController = playerDog.GetComponent<DogController>();
                    if (dogController != null)
                    {
                        dogController.SetPlayerData(player);
                        Debug.Log($"[Player Update] Updated dog display for {playerName}");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning($"[Player Update] Player not found: {playerName}");
        }
    }

    // --- Phase Transition Helpers ---
    
    private string GetPhaseDisplayName(GamePhase phase)
    {
        switch (phase)
        {
            case GamePhase.Joining:
                return "Waiting for Players";
            case GamePhase.GamePhase:
                return "Game Phase";
            case GamePhase.PointPhase:
                return "Point Phase";
            default:
                return phase.ToString();
        }
    }

    // --- Phase Management ---
    
    public void SetPhase(GamePhase newPhase)
    {
        // Don't show transition if it's the same phase
        if (currentPhase == newPhase)
        {
            Debug.Log($"Phase is already {currentPhase}, skipping transition");
            return;
        }
        
        // Start transition coroutine
        StartCoroutine(SetPhaseWithTransition(newPhase));
    }
    
    private IEnumerator SetPhaseWithTransition(GamePhase newPhase)
    {
        // Show phase transition animation
        yield return StartCoroutine(ShowPhaseTransition(newPhase));
        
        // Update the actual phase after transition
        currentPhase = newPhase;
        UpdatePhaseUI();
        Debug.Log($"Phase changed to: {currentPhase}");
    }
    
    private void UpdatePhaseUI()
    {
        // Only update phase display if we're not in a server-controlled game state
        if (currentServerState == ServerGameState.JOINING && phaseDisplay != null)
        {
            phaseDisplay.text = $"Phase: {currentPhase}";
        }
        
        // Update button visibility based on phase, server state, and host status
        bool isHost = NetworkScript.Instance != null && NetworkScript.Instance.isHost;
        
        if (startGameButton != null)
        {
            // Hide start button if game has started (server state is no longer JOINING) or if not in Joining phase
            bool shouldShowStartButton = currentPhase == GamePhase.Joining && 
                                       currentServerState == ServerGameState.JOINING && 
                                       isHost;
            startGameButton.gameObject.SetActive(shouldShowStartButton);
        }
        
        if (nextPhaseButton != null)
        {
            nextPhaseButton.gameObject.SetActive(currentPhase != GamePhase.Joining && isHost);
            nextPhaseButton.GetComponentInChildren<TextMeshProUGUI>().text = 
                currentPhase == GamePhase.GamePhase ? "End Game Phase" : "Next Phase";
        }
        
        Debug.Log($"UpdatePhaseUI called - currentPhase: {currentPhase}, serverState: {currentServerState}");
    }
    
    public void StartGame()
    {
        if (NetworkScript.Instance != null && NetworkScript.Instance.isHost && currentPhase == GamePhase.Joining)
        {
            Debug.Log("[GameManager] StartGame button clicked. Telling NetworkScript to emit the event.");

            NetworkScript.Instance.StartGame();
        }
    }
    
    public void NextPhase()
    {
        if (NetworkScript.Instance.isHost)
        {
            switch (currentPhase)
            {
                case GamePhase.GamePhase:
                    SetPhase(GamePhase.PointPhase);
                    break;
                case GamePhase.PointPhase:
                    SetPhase(GamePhase.GamePhase);
                    break;
            }
        }
    }
    
    private bool HasPlayerListChanged(List<string> newPlayerList)
    {
        if (lastPlayerList.Count != newPlayerList.Count)
            return true;
            
        for (int i = 0; i < lastPlayerList.Count; i++)
        {
            if (lastPlayerList[i] != newPlayerList[i])
                return true;
        }
        
        return false;
    }
    
    // Method to return to main menu
    public void ReturnToMainMenu()
    {
        // Optionally disconnect from the lobby
        if (NetworkScript.Instance != null && NetworkScript.Instance.socket != null)
        {
            // You can emit a "leaveLobby" event here if your server supports it
            Debug.Log("Leaving lobby...");
        }
        
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
    
    // --- Testing Methods ---
    
    [ContextMenu("Test Trivia UI")]
    public void TestTriviaUI()
    {
        Debug.Log("Testing trivia UI with sample data");
        
        // Create test trivia data
        var testTrivia = new TriviaData
        {
            question = "What is the capital of France?",
            options = new string[] { "Paris", "London", "Berlin", "Madrid" },
            correctAnswerIndex = 0
        };
        
        UpdateServerGameState(ServerGameState.TRIVIA, testTrivia);
    }
    
    [ContextMenu("Test Prompt UI")]
    public void TestPromptUI()
    {
        Debug.Log("Testing prompt waiting UI - trivia elements should be hidden");
        UpdateServerGameState(ServerGameState.PROMPT);
    }
    
    [ContextMenu("Test Loading UI")]
    public void TestLoadingUI()
    {
        Debug.Log("Testing loading UI - trivia elements should be hidden");
        UpdateServerGameState(ServerGameState.GENERATING);
    }
    
    [ContextMenu("Hide All UI")]
    public void TestHideAllUI()
    {
        Debug.Log("Hiding all UI elements");
        HideAllGameStatePanels();
    }
    
    [ContextMenu("Check Prompt Panel State")]
    public void CheckPromptPanelState()
    {
        if (promptWaitingPanel != null)
        {
            bool isActive = promptWaitingPanel.activeInHierarchy;
            bool isActiveSelf = promptWaitingPanel.activeSelf;
            Debug.Log($"Prompt Panel - activeInHierarchy: {isActive}, activeSelf: {isActiveSelf}");
            Debug.Log($"Current Server State: {currentServerState}");
            
            // Check parent state
            Transform parent = promptWaitingPanel.transform.parent;
            if (parent != null)
            {
                Debug.Log($"Prompt Panel Parent '{parent.name}' - active: {parent.gameObject.activeInHierarchy}");
            }
        }
        else
        {
            Debug.LogError("Prompt waiting panel is null!");
        }
    }
    
    [ContextMenu("Force Hide Prompt Panel")]
    public void ForceHidePromptPanel()
    {
        if (promptWaitingPanel != null)
        {
            promptWaitingPanel.SetActive(false);
            Debug.Log("Force hidden prompt panel");
        }
        else
        {
            Debug.LogError("Cannot force hide - prompt panel is null!");
        }
    }
    
    [ContextMenu("Test Fade In Prompt")]
    public void TestFadeInPrompt()
    {
        Debug.Log("Testing prompt fade in");
        FadeInPromptPanel();
    }
    
    [ContextMenu("Test Fade Out Prompt")]
    public void TestFadeOutPrompt()
    {
        Debug.Log("Testing prompt fade out");
        FadeOutPromptPanel();
    }
    
    [ContextMenu("Test Fade In Trivia")]
    public void TestFadeInTrivia()
    {
        Debug.Log("Testing trivia fade in");
        // Set some test content first
        var testTrivia = new TriviaData
        {
            question = "Test Question",
            options = new string[] { "A", "B", "C", "D" }
        };
        currentTriviaData = testTrivia;
        SetTriviaContent();
        FadeInTriviaElements();
    }
    
    [ContextMenu("Test Fade Out Trivia")]
    public void TestFadeOutTrivia()
    {
        Debug.Log("Testing trivia fade out");
        FadeOutTriviaElements();
    }
    
    [ContextMenu("Test Long Question")]
    public void TestLongQuestion()
    {
        Debug.Log("Testing long question with auto-sizing");
        var testTrivia = new TriviaData
        {
            question = "This is a very long question that should test the dynamic font sizing system to ensure it fits properly within the designated area without overflowing or becoming unreadable",
            options = new string[] { 
                "This is a very long answer option A that should test the auto-sizing", 
                "Short B", 
                "This is another very long answer option C that might need font size adjustment", 
                "Short D" 
            }
        };
        currentTriviaData = testTrivia;
        SetTriviaContent();
        FadeInTriviaElements();
    }
    
    [ContextMenu("Test Short Question")]
    public void TestShortQuestion()
    {
        Debug.Log("Testing short question with auto-sizing");
        var testTrivia = new TriviaData
        {
            question = "Short?",
            options = new string[] { "A", "B", "C", "D" }
        };
        currentTriviaData = testTrivia;
        SetTriviaContent();
        FadeInTriviaElements();
    }
    
    [ContextMenu("Reset Font Sizes")]
    public void ResetFontSizes()
    {
        Debug.Log("Resetting all text components to auto-sizing");
        SetupAllTextComponents();
    }
    
    [ContextMenu("Force Hide All Trivia Elements")]
    public void ForceHideAllTriviaElements()
    {
        Debug.Log("Force hiding all trivia elements immediately");
        HideTriviaElementsImmediate();
    }
    
    [ContextMenu("Check All UI States")]
    public void CheckAllUIStates()
    {
        Debug.Log("=== UI STATE CHECK ===");
        
        if (promptWaitingPanel != null)
        {
            Debug.Log($"Prompt Panel: active={promptWaitingPanel.activeInHierarchy}");
        }
        
        if (questionText != null)
        {
            Debug.Log($"Question Text: active={questionText.gameObject.activeInHierarchy}");
        }
        
        if (questionImage != null)
        {
            Debug.Log($"Question Image: active={questionImage.gameObject.activeInHierarchy}");
        }
        
        if (answerAText != null && answerAImage != null)
        {
            Debug.Log($"Answer A - Text: active={answerAText.gameObject.activeInHierarchy}, Image: active={answerAImage.gameObject.activeInHierarchy}");
        }
        
        Debug.Log($"Current Server State: {currentServerState}");
        Debug.Log("=== END UI STATE CHECK ===");
    }
    
    [ContextMenu("Check Canvas Group Alpha States")]
    public void CheckCanvasGroupAlphaStates()
    {
        Debug.Log("=== UI STATES CHECK ===");
        
        if (promptPanelCanvasGroup != null)
        {
            Debug.Log($"Prompt Panel CanvasGroup - Alpha: {promptPanelCanvasGroup.alpha}, Active: {promptPanelCanvasGroup.gameObject.activeInHierarchy}, Interactable: {promptPanelCanvasGroup.interactable}");
        }
        else
        {
            Debug.Log("Prompt Panel CanvasGroup: NULL");
        }
        
        if (triviaCanvasGroup != null)
        {
            Debug.Log($"Trivia CanvasGroup - Alpha: {triviaCanvasGroup.alpha}, Active: {triviaCanvasGroup.gameObject.activeInHierarchy}, Interactable: {triviaCanvasGroup.interactable}");
        }
        else
        {
            Debug.Log("Trivia CanvasGroup: NULL");
        }
        
        // Check individual trivia GameObject active states
        if (questionText != null)
        {
            Debug.Log($"Question Text - Active: {questionText.gameObject.activeInHierarchy}");
        }
        
        if (questionImage != null)
        {
            Debug.Log($"Question Image - Active: {questionImage.gameObject.activeInHierarchy}");
        }
        
        if (answerAText != null && answerAImage != null)
        {
            Debug.Log($"Answer A - Text Active: {answerAText.gameObject.activeInHierarchy}, Image Active: {answerAImage.gameObject.activeInHierarchy}");
        }
        
        if (answerBText != null && answerBImage != null)
        {
            Debug.Log($"Answer B - Text Active: {answerBText.gameObject.activeInHierarchy}, Image Active: {answerBImage.gameObject.activeInHierarchy}");
        }
        
        Debug.Log("=== END UI CHECK ===");
    }
    
    [ContextMenu("Test Phase Transition")]
    public void TestPhaseTransition()
    {
        Debug.Log("Testing phase transition manually");
        SetPhase(GamePhase.GamePhase);
    }
    
    [ContextMenu("Test Server State Transition")]
    public void TestServerStateTransition()
    {
        Debug.Log("Testing server state transition manually");
        UpdateServerGameState(ServerGameState.TRIVIA);
    }
    
    [ContextMenu("Check Player Health Status")]
    public void CheckPlayerHealthStatus()
    {
        Debug.Log("=== PLAYER HEALTH STATUS ===");
        foreach (var player in playersData)
        {
            Debug.Log($"{player.playerName}: Health = {player.health}, Submitted = {player.submitted}");
            
            // Also check the dog's health display
            GameObject playerDog = GetDogForPlayer(player.playerName);
            if (playerDog != null)
            {
                DogController dogController = playerDog.GetComponent<DogController>();
                if (dogController != null && dogController.playerData != null)
                {
                    Debug.Log($"  Dog health display: {dogController.playerData.health}");
                }
            }
        }
        Debug.Log("=== END HEALTH STATUS ===");
    }
    
    [ContextMenu("Check Phase Transition Components")]
    public void CheckPhaseTransitionComponents()
    {
        Debug.Log("=== PHASE TRANSITION COMPONENTS CHECK ===");
        
        if (phaseTransitionDimImage != null)
        {
            Debug.Log($"Phase Dim Image - Active: {phaseTransitionDimImage.gameObject.activeInHierarchy}, Color: {phaseTransitionDimImage.color}");
            Debug.Log($"Phase Dim Image Parent Chain:");
            Transform parent = phaseTransitionDimImage.transform.parent;
            int level = 1;
            while (parent != null && level < 5)
            {
                Debug.Log($"  Level {level}: {parent.name} - Active: {parent.gameObject.activeInHierarchy}");
                parent = parent.parent;
                level++;
            }
        }
        else
        {
            Debug.LogError("Phase Transition Dim Image: NULL");
        }
        
        if (phaseTransitionText != null)
        {
            Debug.Log($"Phase Text - Active: {phaseTransitionText.gameObject.activeInHierarchy}, Text: '{phaseTransitionText.text}'");
            Debug.Log($"Phase Text Parent Chain:");
            Transform parent = phaseTransitionText.transform.parent;
            int level = 1;
            while (parent != null && level < 5)
            {
                Debug.Log($"  Level {level}: {parent.name} - Active: {parent.gameObject.activeInHierarchy}");
                parent = parent.parent;
                level++;
            }
        }
        else
        {
            Debug.LogError("Phase Transition Text: NULL");
        }
        
        Debug.Log("=== END PHASE TRANSITION CHECK ===");
    }
    
    // --- Timer System Methods ---
    
    public void StartTimer(float seconds)
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        
        currentTimerSeconds = seconds;
        isTimerActive = true;
        timerCoroutine = StartCoroutine(TimerCountdown());
        UpdateTimerDisplay();
        
        Debug.Log($"[Timer] Started: {seconds} seconds for state {currentServerState}");
    }
    
    public void StopTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        
        isTimerActive = false;
        currentTimerSeconds = 0f;
        UpdateTimerDisplay();
        
        Debug.Log("Timer stopped");
    }
    
    private IEnumerator TimerCountdown()
    {
        while (currentTimerSeconds > 0f && isTimerActive)
        {
            yield return new WaitForSeconds(1f);
            currentTimerSeconds -= 1f;
            UpdateTimerDisplay();
        }
        
        if (isTimerActive)
        {
            // Timer reached zero
            isTimerActive = false;
            currentTimerSeconds = 0f;
            UpdateTimerDisplay();
            Debug.Log("Timer reached zero");
        }
    }
    
    private void UpdateTimerDisplay()
    {
        if (roundTimerDisplay != null)
        {
            if (isTimerActive && currentTimerSeconds > 0f)
            {
                int minutes = Mathf.FloorToInt(currentTimerSeconds / 60f);
                int seconds = Mathf.FloorToInt(currentTimerSeconds % 60f);
                
                // Just show the timer without category text
                roundTimerDisplay.text = $"{minutes:00}:{seconds:00}";
                
                // Change color based on remaining time
                if (currentTimerSeconds <= 10f)
                {
                    roundTimerDisplay.color = Color.red; // Urgent - red
                }
                else if (currentTimerSeconds <= 30f)
                {
                    roundTimerDisplay.color = Color.yellow; // Warning - yellow
                }
                else
                {
                    roundTimerDisplay.color = Color.white; // Normal - white
                }
            }
            else
            {
                roundTimerDisplay.text = "--:--";
                roundTimerDisplay.color = Color.white;
            }
        }
    }
    
    // Update server state handling to start timers
    private void HandleTimerFromServerData(object data)
    {
        float timerValue = 0f;
        
        // Extract timer based on the current server state and data type
        if (data is TriviaData triviaData)
        {
            timerValue = triviaData.timer;
            Debug.Log($"[Timer] TRIVIA state - timer from server: {timerValue}s");
        }
        else if (data is RewardData rewardData)
        {
            timerValue = rewardData.timer;
            Debug.Log($"[Timer] REWARD state - timer from server: {timerValue}s");
        }
        else if (data is GameStateData gameStateData)
        {
            timerValue = gameStateData.timer;
            Debug.Log($"[Timer] {currentServerState} state - timer from server: {timerValue}s");
        }
        
        // Handle timer based on current server state
        switch (currentServerState)
        {
            case ServerGameState.PROMPT:
                // Backend sends 60s for prompt phase
                if (timerValue > 0f)
                {
                    StartTimer(timerValue);
                }
                else
                {
                    Debug.LogWarning("[Timer] No timer provided for PROMPT state, using default 60s");
                    StartTimer(60f); // Fallback to 60 seconds
                }
                break;
                
            case ServerGameState.GENERATING:
                // No timer during generation phase
                Debug.Log("[Timer] GENERATING state - stopping timer");
                StopTimer();
                break;
                
            case ServerGameState.TRIVIA:
                // Backend currently sends PROMPT_PHASE_SECONDS (60) but should be ROUND_TIMER_SECONDS (60)
                // For now, both are 60s so it works, but let's be explicit
                if (timerValue > 0f)
                {
                    StartTimer(timerValue);
                }
                else
                {
                    Debug.LogWarning("[Timer] No timer provided for TRIVIA state, using default 60s");
                    StartTimer(60f); // Fallback to 60 seconds
                }
                break;
                
            case ServerGameState.REWARD:
                // Backend sends 20s for reward phase
                if (timerValue > 0f)
                {
                    StartTimer(timerValue);
                }
                else
                {
                    Debug.LogWarning("[Timer] No timer provided for REWARD state, using default 20s");
                    StartTimer(20f); // Fallback to 20 seconds
                }
                break;
                
            case ServerGameState.ENDGAME:
                // No timer for endgame
                Debug.Log("[Timer] ENDGAME state - stopping timer");
                StopTimer();
                break;
                
            case ServerGameState.JOINING:
                // No timer for joining
                StopTimer();
                break;
                
            default:
                Debug.LogWarning($"[Timer] Unknown server state: {currentServerState}");
                StopTimer();
                break;
        }
    }
}