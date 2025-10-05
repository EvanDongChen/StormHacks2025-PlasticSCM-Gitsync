using UnityEngine;

public class RightHandFSM : MonoBehaviour
{
    [Header("Movement")]
    public Transform hoverPosition;
    public float moveSpeed = 5f;

    private Animator animator;
    private bool animationTriggered = false;
    private Transform currentTarget;

    void Start()
    {
        animator = GetComponent<Animator>();
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!gameObject.activeSelf || currentTarget == null)
            return;

        MoveTo(currentTarget.position);

        if (!animationTriggered && Vector3.Distance(transform.position, currentTarget.position) < 0.1f)
        {
            TriggerAnimation();
        }
    }

    private void MoveTo(Vector3 position)
    {
        transform.position = Vector3.MoveTowards(transform.position, position, moveSpeed * Time.deltaTime);
    }

    private void TriggerAnimation()
    {
        animationTriggered = true;
        animator.SetTrigger("HoverTrigger");
    }

    public void OnHostStateChanged(HostFSM.HostState hostState)
    {
        if (hostState == HostFSM.HostState.Idle)
        {
            gameObject.SetActive(true);
            currentTarget = hoverPosition;
            animationTriggered = false;
        }
        else
        {
            gameObject.SetActive(false);
            currentTarget = null;
        }
    }
}
