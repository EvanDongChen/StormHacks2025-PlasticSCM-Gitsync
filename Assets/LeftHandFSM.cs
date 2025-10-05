using UnityEngine;

public class LeftHandFSM : MonoBehaviour
{
    public enum HandState
    {
        Join,
        Prompt
    }

    public HandState currentState = HandState.Join;

    [Header("Positions")]
    public Transform joinPosition;
    public Transform promptPosition;

    [Header("Movement")]
    public float moveSpeed = 5f;
    private Transform targetPosition;
    private Animator animator;

    private bool animationTriggered = false;
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

    public void OnHostStateChanged(HostFSM.HostState hostState)
    {
        switch (hostState)
        {
            case HostFSM.HostState.Idle:
                SetState(HandState.Join);
                break;
            case HostFSM.HostState.Pointing:
                SetState(HandState.Join);
                break;
            case HostFSM.HostState.ShowingPrompt:
                SetState(HandState.Prompt);
                break;
        }
    }

    public void SetState(HandState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case HandState.Join:
                targetPosition = joinPosition;
                break;
            case HandState.Prompt:
                targetPosition = promptPosition;
                break;
        }

        animationTriggered = false;
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
            case HandState.Join:
                animator.SetTrigger("JoinTrigger");
                break;
            case HandState.Prompt:
                animator.SetTrigger("PromptTrigger");
                break;
        }
    }
}
