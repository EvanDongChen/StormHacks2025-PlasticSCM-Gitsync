using UnityEngine;

public class ChatText : MonoBehaviour
{
    public float floatSpeed = 2f;
    public float lifetime = 2f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;
    }
}
