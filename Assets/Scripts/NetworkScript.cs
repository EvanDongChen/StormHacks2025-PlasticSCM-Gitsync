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

        // --- Tell the socket to start connecting ---
        Debug.Log("Connecting to server...");
        UpdateConnectionStatus("Connecting...");
        socket.Connect();
    }

    void Update()
    {
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
}