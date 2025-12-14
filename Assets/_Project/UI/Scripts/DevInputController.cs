using UnityEngine;
using UnityEngine.InputSystem;

public class DevInputController : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            AlertBus.RaiseAlert(new AlertData
            {
                type = AlertType.Info,
                message = "This is an info alert."
            });
        }
        else if (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            AlertBus.RaiseAlert(new AlertData
            {
                type = AlertType.Warning,
                message = "This is a warning alert."
            });
        }
        else if (Keyboard.current != null && Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            AlertBus.RaiseAlert(new AlertData
            {
                type = AlertType.Critical,
                message = "This is a critical alert."
            });
        }
        else if (Keyboard.current != null && Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            AlertBus.RaiseAlert(new AlertData
            {
                type = AlertType.ChatRes,
                message = "This is a chat alert for residents."
            });
        }
        else if (Keyboard.current != null && Keyboard.current.digit5Key.wasPressedThisFrame)
        {
            AlertBus.RaiseAlert(new AlertData
            {
                type = AlertType.ChatCorp,
                message = "This is a chat alert for corporations."
            });
        }
        else if (Keyboard.current != null && Keyboard.current.digit6Key.wasPressedThisFrame)
        {
            AlertBus.RaiseAlert(new AlertData
            {
                type = AlertType.ChatPol,
                message = "This is a chat alert for politicians."
            });
        }
    }
}
