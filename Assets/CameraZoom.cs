using UnityEngine;

public class CameraZoom : MonoBehaviour
{
    public float minZoom = 5f; //closest zoom
    public float maxZoom = 10f; //farthest zoom
    public float zoomSpeed = 2f; //speed of zooming

    private bool zoomingIn = true;

    void Update()
    {
        if (zoomingIn)
        {
            Camera.main.orthographicSize -= zoomSpeed * Time.deltaTime;
            if (Camera.main.orthographicSize <= minZoom)
            {
                zoomingIn = false;
            }
        }

        else
        {
            Camera.main.orthographicSize += zoomSpeed * Time.deltaTime;
            if (Camera.main.orthographicSize >= maxZoom)
            {
                zoomingIn = true;
            }
        }
    }
}
