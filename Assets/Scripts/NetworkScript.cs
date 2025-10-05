using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using SocketIOClient;
using System;
using System.Collections.Generic;

public class NetworkScript : MonoBehaviour
{
    // Static instance for singleton pattern
    public static NetworkScript Instance { get; private set; }

    // You can set this in the Unity Inspector
    public string connectionURL = "https://storm-hacks-2025.onrender.com/";
    public SocketIOUnity socket;
    
    // UI References - assign these in the inspector
    [Header("UI References")]
    public Button createLobbyButton;
    public TextMeshProUGUI lobbyCodeDisplay;
    public TextMeshProUGUI connectionStatusText;
    public LobbyScript lobbyScript; // Reference to handle fade effect
    
    [Header("Scene Management")]
    public string gameSceneName = "GameScene";
    public float sceneTransitionDelay = 2f; // Wait 2 seconds to show lobby code before transitioning
    
    // Current lobby information
    [Header("Lobby Info")]
    public string currentLobbyCode = "";
    public bool isHost = false;
    public List<string> playersInLobby = new List<string>();
    
    // Queue for main thread updates
    private readonly Queue<System.Action> mainThreadQueue = new Queue<System.Action>();
    private string pendingLobbyCode = "";
    private bool shouldUpdateLobbyDisplay = false;
    private string pendingConnectionStatus = "";
    private bool shouldUpdateConnectionStatus = false;
    private bool pendingButtonState = false;
    private bool shouldUpdateButtonState = false;
    private bool shouldTransitionToGame = false;
    private float transitionTimer = 0f;
    private bool shouldUpdatePlayerList = false;
    private List<string> pendingPlayerList = new List<string>();

    void Awake()
    {
        // Simple instance tracking without destruction
        Debug.Log($"ServerScript Awake called on {gameObject.name}");
        
        if (Instance == null)
        {
            Debug.Log("Setting this as the ServerScript instance");
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning($"Multiple ServerScript instances detected! Keeping the original one.");
            // Don't destroy - just log the warning for main menu flow
        }
    }

    void Start()
    {
        // Set up UI button listeners
        if (createLobbyButton != null)
        {
            createLobbyButton.onClick.AddListener(CreateLobby);
        }
        
        // Initialize UI
        UpdateUI();
        
        var uri = new Uri(connectionURL);
        socket = new SocketIOUnity(uri, new SocketIOOptions
        {
            // We use WebSocket as the transport protocol
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        });

        // --- Set up your event listeners here ---
        socket.OnConnected += (sender, e) =>
        {
            Debug.Log("✅ SUCCESS: Connected to the server!");
            socket.Emit("registerHost");
            pendingConnectionStatus = "Connected";
            shouldUpdateConnectionStatus = true;
            pendingButtonState = true;
            shouldUpdateButtonState = true;
        };

        socket.OnDisconnected += (sender, e) =>
        {
            Debug.Log("❌ Disconnected from server.");
            pendingConnectionStatus = "Disconnected";
            shouldUpdateConnectionStatus = true;
            pendingButtonState = false;
            shouldUpdateButtonState = true;
        };

        // Custom game events
        socket.On("lobbyCreated", response =>
        {
            // Store data for main thread processing
            try
            {
                var lobbyData = response.GetValue();
                string jsonString = lobbyData.ToString();
                string lobbyCode = ExtractLobbyCode(jsonString);
                
                if (!string.IsNullOrEmpty(lobbyCode) && lobbyCode != "UNKNOWN" && lobbyCode != "ERROR")
                {
                    pendingLobbyCode = lobbyCode;
                    shouldUpdateLobbyDisplay = true;
                    Debug.Log($"Lobby Created! Code: {lobbyCode}");
                    
                    // Schedule transition to game scene (main thread safe)
                    shouldTransitionToGame = true;
                    transitionTimer = Time.time + sceneTransitionDelay;
                }
                else
                {
                    Debug.LogWarning("Failed to extract lobby code from server response");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error handling lobbyCreated event: {e.Message}");
            }
        });

        socket.On("lobbyUpdate", response =>
        {
            try
            {
                var lobbyData = response.GetValue();
                string jsonString = lobbyData.ToString();
                Debug.Log($"Lobby Update Received: {jsonString}");
                
                // Extract player list from lobby update
                var playerNames = ExtractPlayerNames(jsonString);
                if (playerNames.Count > 0)
                {
                    pendingPlayerList = playerNames;
                    shouldUpdatePlayerList = true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error handling lobbyUpdate event: {e.Message}");
            }
        });

        socket.On("gameStateChange", response =>
        {
            try
            {
                var data = response.GetValue();
                string jsonString = data.ToString();
                Debug.Log($"GameState changed: {jsonString}");
                
                // Parse the game state change and forward to GameManager
                var gameStateData = ParseGameStateChange(jsonString);
                if (gameStateData != null)
                {
                    Debug.Log($"Parsed game state: {gameStateData.state}");
                    // Queue the game state update for main thread
                    mainThreadQueue.Enqueue(() => {
                        if (GameManager.Instance != null)
                        {
                            Debug.Log($"Forwarding game state to GameManager: {gameStateData.state}");
                            GameManager.Instance.UpdateServerGameState(gameStateData.state, gameStateData.data);
                        }
                        else
                        {
                            Debug.LogError("GameManager.Instance is null when trying to update game state!");
                        }
                    });
                }
                else
                {
                    Debug.LogError("Failed to parse game state change data");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error handling gameStateChange event: {e.Message}");
            }
        });

        socket.On("playerSubmittedUpdate", response =>
        {
            try
            {
                var data = response.GetValue();
                string jsonString = data.ToString();
                Debug.Log($"Player submitted update: {jsonString}");
                
                // Parse player submission updates and forward to GameManager
                var players = ExtractPlayerSubmissionData(jsonString);
                if (players != null && players.Count > 0)
                {
                    // Queue the player update for main thread
                    mainThreadQueue.Enqueue(() => {
                        if (GameManager.Instance != null)
                        {
                            foreach (var player in players)
                            {
                                // Update complete player data including health, not just submission status
                                GameManager.Instance.UpdatePlayerData(
                                    player.playerName, 
                                    health: player.health, 
                                    submitted: player.submitted
                                );
                            }
                        }
                    });
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error handling playerSubmittedUpdate event: {e.Message}");
            }
        });

        // --- Tell the socket to start connecting ---
        Debug.Log("Connecting to server...");
        UpdateConnectionStatus("Connecting...");
        socket.Connect();
    }

    void Update()
    {
        // Process main thread queue
        while (mainThreadQueue.Count > 0)
        {
            var action = mainThreadQueue.Dequeue();
            action?.Invoke();
        }
        
        // Handle UI updates on main thread
        if (shouldUpdateLobbyDisplay)
        {
            currentLobbyCode = pendingLobbyCode;
            isHost = true;
            UpdateLobbyDisplay(pendingLobbyCode);
            shouldUpdateLobbyDisplay = false;
        }
        
        if (shouldUpdateConnectionStatus)
        {
            UpdateConnectionStatus(pendingConnectionStatus);
            shouldUpdateConnectionStatus = false;
        }
        
        if (shouldUpdateButtonState)
        {
            if (createLobbyButton != null)
                createLobbyButton.interactable = pendingButtonState;
            shouldUpdateButtonState = false;
        }
        
        // Handle scene transition on main thread
        if (shouldTransitionToGame && Time.time >= transitionTimer)
        {
            shouldTransitionToGame = false;
            TransitionToGameScene();
        }
        
        // Handle player list updates on main thread
        if (shouldUpdatePlayerList)
        {
            playersInLobby = new List<string>(pendingPlayerList);
            shouldUpdatePlayerList = false;
            Debug.Log($"Updated player list: {string.Join(", ", playersInLobby)}");
        }
    }

    // --- Create public methods to send events to the server ---

    [ContextMenu("Test Create Lobby")]
    public void CreateLobby()
    {
        Debug.Log("CreateLobby button clicked!");
        
        if (socket == null || !socket.Connected) 
        {
            Debug.LogWarning("Cannot create lobby: Not connected to server");
            // For testing - simulate a lobby code
            UpdateLobbyDisplay("TEST");
            return;
        }
        
        Debug.Log("Creating lobby...");
        socket.Emit("createLobby");
    }
    
    // --- Scene Management ---
    
    private void TransitionToGameScene()
    {
        Debug.Log($"Transitioning to game scene: {gameSceneName}");
        
        // Make sure the ServerScript persists across scenes
        DontDestroyOnLoad(gameObject);
        
        // Use fade effect if LobbyScript is available
        if (lobbyScript != null)
        {
            Debug.Log("Using fade transition from LobbyScript");
            lobbyScript.StartGameWithFade();
        }
        else
        {
            // Fallback to direct scene loading if no LobbyScript
            Debug.LogWarning("LobbyScript not assigned, loading scene directly without fade");
            SceneManager.LoadScene(gameSceneName);
        }
    }
    
    // Public method to manually transition (for testing)
    [ContextMenu("Load Game Scene")]
    public void LoadGameScene()
    {
        TransitionToGameScene();
    }

    // --- Game Methods ---
    public void StartGame()
    {
        if (socket != null && socket.Connected && isHost && !string.IsNullOrEmpty(currentLobbyCode))
        {
            Debug.Log("[NetworkScript] Emitting startGame event to server...");
            socket.Emit("startGame", new { lobbyCode  = currentLobbyCode });
        }
        else
        {
            Debug.LogWarning("[NetworkScript] Cannot start game: Not connected, not the host, or no lobby code.");
        }
    }

    // --- UI Helper Methods ---
    
    private void UpdateUI()
    {
        UpdateConnectionStatus("Not Connected");
        UpdateLobbyDisplay("");
        if (createLobbyButton != null)
            createLobbyButton.interactable = true; // Always enable for testing
    }
    
    private void UpdateConnectionStatus(string status)
    {
        if (connectionStatusText != null)
        {
            connectionStatusText.text = $"Status: {status}";
        }
    }
    
    private void UpdateLobbyDisplay(string lobbyCode)
    {
        if (lobbyCodeDisplay != null)
        {
            if (string.IsNullOrEmpty(lobbyCode))
            {
                lobbyCodeDisplay.text = "No Lobby";
            }
            else
            {
                lobbyCodeDisplay.text = $"Lobby Code: {lobbyCode}";
            }
        }
    }
    
    private string ExtractLobbyCode(string jsonResponse)
    {
        try
        {
            // Simple parsing for lobby code - you might want to use a JSON library for more complex parsing
            if (jsonResponse.Contains("lobbyCode"))
            {
                int startIndex = jsonResponse.IndexOf("\"lobbyCode\":\"") + 13;
                int endIndex = jsonResponse.IndexOf("\"", startIndex);
                if (startIndex > 12 && endIndex > startIndex)
                {
                    return jsonResponse.Substring(startIndex, endIndex - startIndex);
                }
            }
            return "UNKNOWN";
        }
        catch
        {
            return "ERROR";
        }
    }
    
    private List<string> ExtractPlayerNames(string jsonResponse)
    {
        var playerNames = new List<string>();
        try
        {
            // Look for players array in the JSON
            if (jsonResponse.Contains("\"players\":"))
            {
                int playersStart = jsonResponse.IndexOf("\"players\":");
                if (playersStart != -1)
                {
                    // Find the opening bracket of the players array
                    int arrayStart = jsonResponse.IndexOf("[", playersStart);
                    int arrayEnd = jsonResponse.IndexOf("]", arrayStart);
                    
                    if (arrayStart != -1 && arrayEnd != -1)
                    {
                        string playersSection = jsonResponse.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
                        
                        // Extract player names (simple parsing)
                        string[] playerObjects = playersSection.Split(new string[] { "},{" }, StringSplitOptions.RemoveEmptyEntries);
                        
                        foreach (string playerObj in playerObjects)
                        {
                            if (playerObj.Contains("\"name\":"))
                            {
                                int nameStart = playerObj.IndexOf("\"name\":\"") + 8;
                                int nameEnd = playerObj.IndexOf("\"", nameStart);
                                if (nameStart > 7 && nameEnd > nameStart)
                                {
                                    string playerName = playerObj.Substring(nameStart, nameEnd - nameStart);
                                    playerNames.Add(playerName);
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error extracting player names: {e.Message}");
        }
        
        return playerNames;
    }

    // Data structure for parsed game state changes
    private class GameStateChangeData
    {
        public ServerGameState state;
        public object data;
    }

    // Simple player data structure for network updates
    private class SimplePlayerData
    {
        public string playerName;
        public bool submitted;
        public int health;
    }

    private GameStateChangeData ParseGameStateChange(string jsonResponse)
    {
        try
        {
            var result = new GameStateChangeData();
            
            // Extract state
            if (jsonResponse.Contains("\"state\":"))
            {
                int stateStart = jsonResponse.IndexOf("\"state\":\"") + 9;
                int stateEnd = jsonResponse.IndexOf("\"", stateStart);
                if (stateStart > 8 && stateEnd > stateStart)
                {
                    string stateString = jsonResponse.Substring(stateStart, stateEnd - stateStart);
                    
                    // Convert to ServerGameState enum
                    if (System.Enum.TryParse<ServerGameState>(stateString, true, out ServerGameState gameState))
                    {
                        result.state = gameState;
                    }
                    else
                    {
                        Debug.LogWarning($"Unknown game state: {stateString}");
                        return null;
                    }
                }
            }
            
            // Parse additional data based on state
            switch (result.state)
            {
                case ServerGameState.TRIVIA:
                    result.data = ParseTriviaData(jsonResponse);
                    break;
                case ServerGameState.REWARD:
                    result.data = ParseRewardData(jsonResponse);
                    break;
                case ServerGameState.ENDGAME:
                    result.data = ParseEndGameData(jsonResponse);
                    break;
            }
            
            return result;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing game state change: {e.Message}");
            return null;
        }
    }

    private TriviaData ParseTriviaData(string jsonResponse)
    {
        var triviaData = new TriviaData();
        
        try
        {
            // Check for new server format: "data": ["question", "optionA", "optionB", "optionC", "optionD", "correctAnswer"]
            if (jsonResponse.Contains("\"data\":"))
            {
                var dataArray = ExtractDataArray(jsonResponse);
                if (dataArray != null && dataArray.Length >= 5)
                {
                    triviaData.question = dataArray[0];
                    triviaData.options = new string[4];
                    triviaData.options[0] = dataArray[1]; // Option A
                    triviaData.options[1] = dataArray[2]; // Option B  
                    triviaData.options[2] = dataArray[3]; // Option C
                    triviaData.options[3] = dataArray[4]; // Option D
                    
                    // Find correct answer index (optional - for client-side validation)
                    if (dataArray.Length >= 6)
                    {
                        string correctAnswer = dataArray[5];
                        for (int i = 0; i < 4; i++)
                        {
                            if (triviaData.options[i] == correctAnswer)
                            {
                                triviaData.correctAnswerIndex = i;
                                break;
                            }
                        }
                    }
                    
                    Debug.Log($"Parsed trivia: Q='{triviaData.question}' A='{triviaData.options[0]}' B='{triviaData.options[1]}' C='{triviaData.options[2]}' D='{triviaData.options[3]}'");
                    return triviaData;
                }
            }
            
            // Fallback: Check for old format with separate question and options fields
            if (jsonResponse.Contains("\"question\":"))
            {
                int questionStart = jsonResponse.IndexOf("\"question\":\"") + 12;
                int questionEnd = jsonResponse.IndexOf("\"", questionStart);
                if (questionStart > 11 && questionEnd > questionStart)
                {
                    triviaData.question = jsonResponse.Substring(questionStart, questionEnd - questionStart);
                }
            }
            
            if (jsonResponse.Contains("\"options\":"))
            {
                triviaData.options = ExtractOptionsArray(jsonResponse);
            }
            
            return triviaData;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing trivia data: {e.Message}");
            return null;
        }
    }

    private string[] ExtractOptionsArray(string jsonResponse)
    {
        var options = new string[4];
        try
        {
            // Simple parsing for options array - looking for pattern like ["A","B","C","D"]
            int optionsStart = jsonResponse.IndexOf("\"options\":");
            if (optionsStart != -1)
            {
                int arrayStart = jsonResponse.IndexOf("[", optionsStart);
                int arrayEnd = jsonResponse.IndexOf("]", arrayStart);
                
                if (arrayStart != -1 && arrayEnd != -1)
                {
                    string optionsSection = jsonResponse.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
                    
                    // Split by comma and clean up quotes
                    string[] parts = optionsSection.Split(',');
                    for (int i = 0; i < parts.Length && i < 4; i++)
                    {
                        string option = parts[i].Trim().Trim('"');
                        options[i] = option;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error extracting options array: {e.Message}");
        }
        
        return options;
    }

    private string[] ExtractDataArray(string jsonResponse)
    {
        try
        {
            // Look for "data": [...] pattern
            int dataStart = jsonResponse.IndexOf("\"data\":");
            if (dataStart != -1)
            {
                int arrayStart = jsonResponse.IndexOf("[", dataStart);
                int arrayEnd = jsonResponse.IndexOf("]", arrayStart);
                
                if (arrayStart != -1 && arrayEnd != -1)
                {
                    string dataSection = jsonResponse.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
                    
                    // Split by comma and clean up quotes
                    string[] parts = dataSection.Split(',');
                    string[] cleanedParts = new string[parts.Length];
                    
                    for (int i = 0; i < parts.Length; i++)
                    {
                        string part = parts[i].Trim().Trim('"');
                        cleanedParts[i] = part;
                    }
                    
                    Debug.Log($"Extracted data array: [{string.Join(", ", cleanedParts)}]");
                    return cleanedParts;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error extracting data array: {e.Message}");
        }
        
        return null;
    }

    private RewardData ParseRewardData(string jsonResponse)
    {
        var rewardData = new RewardData();
        
        try
        {
            // Extract solutionIndex
            if (jsonResponse.Contains("\"solutionIndex\":"))
            {
                int solutionStart = jsonResponse.IndexOf("\"solutionIndex\":") + 16;
                int solutionEnd = jsonResponse.IndexOfAny(new char[] { ',', '}' }, solutionStart);
                if (solutionStart > 15 && solutionEnd > solutionStart)
                {
                    string solutionStr = jsonResponse.Substring(solutionStart, solutionEnd - solutionStart);
                    if (int.TryParse(solutionStr, out int solutionIndex))
                    {
                        rewardData.solutionIndex = solutionIndex;
                    }
                }
            }
            
            // For now, we'll update player health in GameManager based on the results
            // More complex parsing can be added here as needed
            
            return rewardData;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing reward data: {e.Message}");
            return null;
        }
    }

    private EndGameData ParseEndGameData(string jsonResponse)
    {
        var endGameData = new EndGameData();
        
        try
        {
            // Extract winner information
            if (jsonResponse.Contains("\"winner\":"))
            {
                int winnerStart = jsonResponse.IndexOf("\"winner\":\"") + 10;
                int winnerEnd = jsonResponse.IndexOf("\"", winnerStart);
                if (winnerStart > 9 && winnerEnd > winnerStart)
                {
                    string winner = jsonResponse.Substring(winnerStart, winnerEnd - winnerStart);
                    endGameData.winners = new string[] { winner };
                }
            }
            
            return endGameData;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing end game data: {e.Message}");
            return null;
        }
    }

    private List<SimplePlayerData> ExtractPlayerSubmissionData(string jsonResponse)
    {
        var players = new List<SimplePlayerData>();
        
        try
        {
            // Look for players array in the JSON
            if (jsonResponse.Contains("\"players\":"))
            {
                int playersStart = jsonResponse.IndexOf("\"players\":");
                if (playersStart != -1)
                {
                    int arrayStart = jsonResponse.IndexOf("[", playersStart);
                    int arrayEnd = jsonResponse.IndexOf("]", arrayStart);
                    
                    if (arrayStart != -1 && arrayEnd != -1)
                    {
                        string playersSection = jsonResponse.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
                        
                        // Parse player objects
                        string[] playerObjects = playersSection.Split(new string[] { "},{" }, StringSplitOptions.RemoveEmptyEntries);
                        
                        foreach (string playerObj in playerObjects)
                        {
                            var player = ParsePlayerFromJson(playerObj);
                            if (player != null)
                            {
                                players.Add(player);
                            }
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error extracting player submission data: {e.Message}");
        }
        
        return players;
    }

    private SimplePlayerData ParsePlayerFromJson(string playerJson)
    {
        try
        {
            string playerName = "";
            bool submitted = false;
            int health = 4;
            
            // Extract name
            if (playerJson.Contains("\"name\":"))
            {
                int nameStart = playerJson.IndexOf("\"name\":\"") + 8;
                int nameEnd = playerJson.IndexOf("\"", nameStart);
                if (nameStart > 7 && nameEnd > nameStart)
                {
                    playerName = playerJson.Substring(nameStart, nameEnd - nameStart);
                }
            }
            
            // Extract submitted status
            if (playerJson.Contains("\"submitted\":"))
            {
                int submittedStart = playerJson.IndexOf("\"submitted\":") + 12;
                int submittedEnd = playerJson.IndexOfAny(new char[] { ',', '}' }, submittedStart);
                if (submittedStart > 11 && submittedEnd > submittedStart)
                {
                    string submittedStr = playerJson.Substring(submittedStart, submittedEnd - submittedStart);
                    bool.TryParse(submittedStr, out submitted);
                }
            }
            
            // Extract health
            if (playerJson.Contains("\"health\":"))
            {
                int healthStart = playerJson.IndexOf("\"health\":") + 9;
                int healthEnd = playerJson.IndexOfAny(new char[] { ',', '}' }, healthStart);
                if (healthStart > 8 && healthEnd > healthStart)
                {
                    string healthStr = playerJson.Substring(healthStart, healthEnd - healthStart);
                    int.TryParse(healthStr, out health);
                }
            }
            
            if (!string.IsNullOrEmpty(playerName))
            {
                return new SimplePlayerData
                {
                    playerName = playerName,
                    submitted = submitted,
                    health = health
                };
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing player from JSON: {e.Message}");
        }
        
        return null;
    }
}