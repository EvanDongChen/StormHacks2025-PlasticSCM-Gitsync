using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class PlayerData
{
    public string playerName;
    public int health = 4;
    public bool isHost = false;
    
    public PlayerData(string name, bool host = false)
    {
        playerName = name;
        isHost = host;
        health = 4;
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
    [Header("Game Phases")]
    public GamePhase currentPhase = GamePhase.Joining;
    
    [Header("Game UI")]
    public TextMeshProUGUI lobbyCodeDisplay;
    public TextMeshProUGUI playerCountDisplay;
    public TextMeshProUGUI playerListDisplay;
    public TextMeshProUGUI phaseDisplay;
    
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
    
    private List<string> lastPlayerList = new List<string>();
    
    void Start()
    {
        // Set up button listeners
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(StartGame);
        }
        
        if (nextPhaseButton != null)
        {
            nextPhaseButton.onClick.AddListener(NextPhase);
        }
        
        // Initialize phase
        SetPhase(GamePhase.Joining);
        
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
                UpdatePlayerDataFromServer();
                UpdatePlayerDisplay();
            }
        }
    }
    
    // --- Player Data Management ---
    
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
            UpdatePlayerDisplay();
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
        
        if (playerCountDisplay != null)
        {
            playerCountDisplay.text = isHost ? "Host" : "Player";
        }
        
        // Initial player display
        UpdatePlayerDisplay();
        
        Debug.Log($"Game started! Lobby: {lobbyCode}, Host: {isHost}");
    }
    
    // --- Phase Management ---
    
    public void SetPhase(GamePhase newPhase)
    {
        currentPhase = newPhase;
        UpdatePhaseUI();
        Debug.Log($"Phase changed to: {currentPhase}");
    }
    
    private void UpdatePhaseUI()
    {
        if (phaseDisplay != null)
        {
            phaseDisplay.text = $"Phase: {currentPhase}";
        }
        
        // Update button visibility based on phase and host status
        bool isHost = NetworkScript.Instance != null && NetworkScript.Instance.isHost;
        
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(currentPhase == GamePhase.Joining && isHost);
        }
        
        if (nextPhaseButton != null)
        {
            nextPhaseButton.gameObject.SetActive(currentPhase != GamePhase.Joining && isHost);
            nextPhaseButton.GetComponentInChildren<TextMeshProUGUI>().text = 
                currentPhase == GamePhase.GamePhase ? "End Game Phase" : "Next Phase";
        }
    }
    
    public void StartGame()
    {
        if (NetworkScript.Instance.isHost && currentPhase == GamePhase.Joining)
        {
            SetPhase(GamePhase.GamePhase);
            Debug.Log("Game started by host!");
            
            // You can emit a socket event to notify other players
            // NetworkScript.Instance.socket.Emit("gameStarted");
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
    
    private void UpdatePlayerDisplay()
    {
        if (playerListDisplay != null)
        {
            if (playersData.Count > 0)
            {
                string playerText = $"Players ({playersData.Count}):\n";
                foreach (PlayerData player in playersData)
                {
                    playerText += $"{player.playerName} - HP: {player.health}\n";
                }
                playerListDisplay.text = playerText.TrimEnd('\n');
            }
            else
            {
                playerListDisplay.text = "Players (0):\nWaiting for players...";
            }
        }
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
}