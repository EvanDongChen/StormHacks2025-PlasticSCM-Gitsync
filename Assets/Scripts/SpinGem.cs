using UnityEngine;

public class SpinGem : MonoBehaviour
{
    public float rotationSpeed = 180f;

    void Update() {
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
    }
}
