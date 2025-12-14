using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleCameraPanZoom : MonoBehaviour
{
    public float panSpeed = 10f;
    public float zoomSpeed = 5f;
    public float minOrthoSize = 2f;
    public float maxOrthoSize = 50f;

    void Update()
    {
        // Pan with WASD / arrow keys the ugly new input system way
        float dx = (Keyboard.current.rightArrowKey.isPressed ? 1f : 0f)
                - (Keyboard.current.leftArrowKey.isPressed  ? 1f : 0f);

        float dy = (Keyboard.current.upArrowKey.isPressed   ? 1f : 0f)
                - (Keyboard.current.downArrowKey.isPressed ? 1f : 0f);

        transform.position += new Vector3(dx, dy, 0f) * panSpeed * Time.deltaTime;

        // Zoom with scroll wheel
        if (Camera.main != null && Camera.main.orthographic)
        {
            // Ugly new Input System way
            float scroll = Mouse.current != null
                ? Mouse.current.scroll.ReadValue().y * 0.1f
                : 0f;
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                Camera.main.orthographicSize -= scroll * zoomSpeed;
                Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, minOrthoSize, maxOrthoSize);
            }
        }
    }
}
