using UnityEngine;

public class SimpleCameraPanZoom : MonoBehaviour
{
    public float panSpeed = 10f;
    public float zoomSpeed = 5f;
    public float minOrthoSize = 2f;
    public float maxOrthoSize = 50f;

    void Update()
    {
        // Pan with WASD / arrow keys
        float dx = Input.GetAxis("Horizontal");
        float dy = Input.GetAxis("Vertical");
        transform.position += new Vector3(dx, dy, 0f) * panSpeed * Time.deltaTime;

        // Zoom with scroll wheel
        if (Camera.main != null && Camera.main.orthographic)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                Camera.main.orthographicSize -= scroll * zoomSpeed;
                Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, minOrthoSize, maxOrthoSize);
            }
        }
    }
}
