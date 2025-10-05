using UnityEngine;

public class HostFSM : MonoBehaviour
{
    public enum HostState
    {
        Idle,
        Welcoming,      // For JOINING state - greeting players
        Pointing,       // For TRIVIA state - pointing at questions
        ShowingPrompt,  // For PROMPT state - presenting the prompt
        Thinking,       // For GENERATING state - waiting/thinking pose
        Attacking,      // For REWARD state - dramatic attack/spell casting
        Celebrating     // For ENDGAME state - victory celebration
    }

    public HostState currentState = HostState.Idle;

    public Transform idlePosition;
    public Transform welcomingPosition;
    public Transform pointingPosition;
    public Transform promptPosition;
    public Transform thinkingPosition;
    public Transform attackingPosition;
    public Transform celebratingPosition;

    public float moveSpeed = 5f;

    private Transform targetPosition;

    private Animator animator;

    private bool animationTriggered = false;

    [Header("Hands")]
    public LeftHandFSM leftHand;
    public RightHandController rightHand;

    void Start()
    {
        animator = GetComponent<Animator>();
        SetState(currentState);
    }

    void Update()
    {
        if (targetPosition != null)
        {
            MoveTo(targetPosition.position);
        }

        if (!animationTriggered && Vector3.Distance(transform.position, targetPosition.position) < 0.1f)
        {
            TriggerAnimation();
        }
    }

    public void SetState(HostState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case HostState.Idle:
                targetPosition = idlePosition;
                break;
            case HostState.Welcoming:
                targetPosition = welcomingPosition;
                break;
            case HostState.Pointing:
                targetPosition = pointingPosition;
                break;
            case HostState.ShowingPrompt:
                targetPosition = promptPosition;
                break;
            case HostState.Thinking:
                targetPosition = thinkingPosition;
                break;
            case HostState.Attacking:
                targetPosition = attackingPosition;
                break;
            case HostState.Celebrating:
                targetPosition = celebratingPosition;
                break;
        }

        animationTriggered = false;

        if (leftHand != null)
        {
            leftHand.OnHostStateChanged(currentState);
        }

        if (rightHand != null)
        {
            rightHand.OnHostStateChanged(currentState);
        }
    }

    private void MoveTo(Vector3 position)
    {
        transform.position = Vector3.MoveTowards(transform.position, position, moveSpeed * Time.deltaTime);
    }

    private void TriggerAnimation()
    {
        animationTriggered = true;

        switch (currentState)
        {
            case HostState.Idle:
                animator.SetTrigger("IdleTrigger");
                break;
            case HostState.Pointing:
                animator.SetTrigger("PointingTrigger");
                break;
            case HostState.ShowingPrompt:
                animator.SetTrigger("ShowPromptTrigger");
                break;
            case HostState.Attacking:
                animator.SetTrigger("AttackTrigger");
                break;
        }
    } 
}
