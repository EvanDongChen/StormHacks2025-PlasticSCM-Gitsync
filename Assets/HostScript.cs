using UnityEngine;

public class HostScript : MonoBehaviour
{
    public enum HostState
    {
        JoiningPhase,
        PromptingPhase,
        TriviaPhase,
        RewardPhase
    }
    
    [Header("Host State Configuration")]
    public HostState currentHostState = HostState.JoiningPhase;
    
    [Header("Position Transforms")]
    public Transform joiningPhasePosition;
    public Transform promptingPhasePosition;
    public Transform triviaPhasePosition;
    public Transform rewardPhasePosition;
    
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    
    [Header("Game State Mapping")]
    public HostState joiningState = HostState.JoiningPhase;
    public HostState promptState = HostState.PromptingPhase;
    public HostState generatingState = HostState.PromptingPhase;   // GENERATING maps to PromptingPhase
    public HostState triviaState = HostState.TriviaPhase;
    public HostState rewardState = HostState.RewardPhase;
    public HostState endgameState = HostState.RewardPhase;        // ENDGAME maps to RewardPhase
    
    private ServerGameState currentGameState = ServerGameState.JOINING;
    private Transform targetPosition;
    private Animator animator;
    private bool animationTriggered = false;
    
    void Start()
    {
        // Get animator component
        animator = GetComponent<Animator>();
        
        // Subscribe to game state changes
        if (GameManager.Instance != null)
        {
            // Set initial state based on current game state
            UpdateHostStateForGameState(GameManager.Instance.currentServerState);
        }
        else
        {
            // Default to joining state
            SetHostState(HostState.JoiningPhase);
        }
    }

    void Update()
    {
        // Constantly check if game state has changed - more aggressive monitoring
        if (GameManager.Instance != null)
        {
            ServerGameState latestState = GameManager.Instance.currentServerState;
            if (latestState != currentGameState)
            {
                Debug.Log($"HostScript detected game state change from {currentGameState} to {latestState}");
                UpdateHostStateForGameState(latestState);
            }
        }
        
        // Handle movement to target position
        if (targetPosition != null)
        {
            MoveTo(targetPosition.position);
        }
        
        // Set animation bools when reaching target position OR immediately if already triggered
        if ((!animationTriggered && targetPosition != null && 
            Vector3.Distance(transform.position, targetPosition.position) < 0.1f) || 
            (animationTriggered && targetPosition != null))
        {
            SetAnimationBools();
        }
    }
    
    /// <summary>
    /// Updates the host's state based on the current game state
    /// </summary>
    /// <param name="gameState">The new game state</param>
    public void UpdateHostStateForGameState(ServerGameState gameState)
    {
        Debug.Log($"HostScript.UpdateHostStateForGameState called with: {gameState}");
        
        currentGameState = gameState;
        
        HostState newHostState = GetHostStateForGameState(gameState);
        
        Debug.Log($"Host state changing from {currentHostState} to {newHostState} for game state {gameState}");
        
        SetHostState(newHostState);
        
        // Immediately update animation bools for the new state
        SetAnimationBools();
    }
    
    /// <summary>
    /// Maps a game state to the appropriate host state
    /// </summary>
    /// <param name="gameState">The current game state</param>
    /// <returns>The corresponding host state</returns>
    private HostState GetHostStateForGameState(ServerGameState gameState)
    {
        switch (gameState)
        {
            case ServerGameState.JOINING:
                return joiningState;
            case ServerGameState.PROMPT:
                return promptState;
            case ServerGameState.GENERATING:
                return generatingState;
            case ServerGameState.TRIVIA:
                return triviaState;
            case ServerGameState.REWARD:
                return rewardState;
            case ServerGameState.ENDGAME:
                return endgameState;
            default:
                Debug.LogWarning($"Unknown game state: {gameState}. Using JoiningPhase host state.");
                return HostState.JoiningPhase;
        }
    }
    
    /// <summary>
    /// Set the host state and handle position/animation changes
    /// </summary>
    /// <param name="newState">The host state to set</param>
    public void SetHostState(HostState newState)
    {
        currentHostState = newState;
        
        // Set target position based on state
        switch (currentHostState)
        {
            case HostState.JoiningPhase:
                targetPosition = joiningPhasePosition;
                break;
            case HostState.PromptingPhase:
                targetPosition = promptingPhasePosition;
                break;
            case HostState.TriviaPhase:
                targetPosition = triviaPhasePosition;
                break;
            case HostState.RewardPhase:
                targetPosition = rewardPhasePosition;
                break;
        }
        
        // Reset animation trigger so it will trigger when reaching the new position
        animationTriggered = false;
        
        Debug.Log($"Host state set to: {currentHostState}");
    }
    
    /// <summary>
    /// Move the host towards a target position
    /// </summary>
    /// <param name="position">The target position</param>
    private void MoveTo(Vector3 position)
    {
        transform.position = Vector3.MoveTowards(transform.position, position, moveSpeed * Time.deltaTime);
    }
    
    /// <summary>
    /// Set the appropriate animation bool for the current state
    /// </summary>
    private void SetAnimationBools()
    {
        animationTriggered = true;
        
        // Double-check we have the latest game state
        if (GameManager.Instance != null)
        {
            currentGameState = GameManager.Instance.currentServerState;
        }
        
        Debug.Log($"SetAnimationBools called - CurrentGameState: {currentGameState}, HostState: {currentHostState}");
        
        if (animator == null)
        {
            Debug.LogWarning("No Animator component found on Host!");
            return;
        }
        
        // Reset all animation bools first
        animator.SetBool("Join", false);
        animator.SetBool("Prompt", false);
        animator.SetBool("Trivia", false);
        animator.SetBool("Reward", false);
        
        // Set the appropriate bool based on server game state
        switch (currentGameState)
        {
            case ServerGameState.JOINING:
                animator.SetBool("Join", true);
                Debug.Log("Set Join animation bool to true for Joining Phase");
                break;
            case ServerGameState.PROMPT:
            case ServerGameState.GENERATING:
                animator.SetBool("Prompt", true);  // Prompt bool for prompt/generating phase
                Debug.Log("Set Prompt animation bool to true for Prompting/Generating Phase");
                break;
            case ServerGameState.TRIVIA:
                animator.SetBool("Trivia", true);
                Debug.Log("Set Trivia animation bool to true for Trivia Phase");
                break;
            case ServerGameState.REWARD:
            case ServerGameState.ENDGAME:
                animator.SetBool("Reward", true);  // Reward bool for reward and processing answers
                Debug.Log("Set Reward animation bool to true for Reward/EndGame Phase");
                break;
            default:
                Debug.LogWarning($"Unknown server game state for animation: {currentGameState}");
                break;
        }
        
        Debug.Log($"Animation bools set for server state: {currentGameState}");
    }
    
    /// <summary>
    /// Get the current host state
    /// </summary>
    /// <returns>The current host state</returns>
    public HostState GetCurrentHostState()
    {
        return currentHostState;
    }
    
    /// <summary>
    /// Force the host to immediately move to a position without animation
    /// </summary>
    /// <param name="position">The position to move to</param>
    public void TeleportTo(Vector3 position)
    {
        transform.position = position;
        animationTriggered = true; // Prevent animation trigger
    }
}

