using System;
using UnityEngine;

public class AlertBus : MonoBehaviour
{
    public static event Action<AlertData> AlertRaised;

    public static void RaiseAlert(AlertData alertData)
    {
        AlertRaised?.Invoke(alertData);
    }
}
