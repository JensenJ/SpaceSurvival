using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipComponent : MonoBehaviour
{
    ShipComponentAsset shipComponentData;
    public static ShipComponent current;

    void Awake()
    {
        current = this;
    }

    public event Action onComponentActivate;
    public void ActivateComponent()
    {
        onComponentActivate?.Invoke();
    }

    public event Action onComponentDeactivate;
    public void DeactivateComponent()
    {
        onComponentDeactivate?.Invoke();
    }
}
