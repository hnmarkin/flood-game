using UnityEngine;

public class DevInputController : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            AlertBus.RaiseAlert(new AlertData
            {
                type = AlertType.Info,
                message = "This is an info alert."
            });
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            AlertBus.RaiseAlert(new AlertData
            {
                type = AlertType.Warning,
                message = "This is a warning alert."
            });
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            AlertBus.RaiseAlert(new AlertData
            {
                type = AlertType.Critical,
                message = "This is a critical alert."
            });
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            AlertBus.RaiseAlert(new AlertData
            {
                type = AlertType.ChatRes,
                message = "This is a chat alert for residents."
            });
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            AlertBus.RaiseAlert(new AlertData
            {
                type = AlertType.ChatCorp,
                message = "This is a chat alert for corporations."
            });
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            AlertBus.RaiseAlert(new AlertData
            {
                type = AlertType.ChatPol,
                message = "This is a chat alert for politicians."
            });
        }
    }
}
