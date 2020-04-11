using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerFlashLight : NetworkBehaviour
{
    [SerializeField] GameObject flashLight = null;
    [SerializeField] [SyncVar(hook = nameof(HookSetStatus))] public bool flashLightStatus = false;

    [SerializeField] [SyncVar(hook = nameof(HookSetBattery))] public float flashLightBattery = 100.0f;
    [SerializeField] [SyncVar(hook = nameof(HookSetMaxBattery))] public float flashLightMaxBattery = 100.0f;

    [SerializeField] public static float baseFlashLightBattery = 100.0f;

    [SerializeField] public float flashLightRechargeRate = 5f;
    [SerializeField] public float flashLightDrainRate = 3f;

    GameObject gameManager = null;

    // Start is called before the first frame update
    public override void OnStartClient()
    {
        base.OnStartClient();

        if (flashLight == null)
        {
            flashLight = transform.GetChild(0).GetChild(0).gameObject;
        }

        if (!hasAuthority)
        {
            return;
        }

        ToggleFlashLight(flashLightStatus);

        gameManager = GameObject.FindGameObjectWithTag("GameController");
    }

    void Update()
    {
        if (!hasAuthority)
        {
            return;
        }

        //Flashlight draining logic
        //If flashlight is on, drain it
        if(flashLightStatus == true)
        {
            DrainFlashLight(flashLightDrainRate * Time.deltaTime);
            if(flashLightBattery <= 0.0f)
            {
                ToggleFlashLight(false);
            }
        }
        //Otherwise, recharge it
        else
        {
            if(GetFlashLightCharge() < GetMaxFlashLightCharge())
            {
                RechargeFlashLight(flashLightRechargeRate * Time.deltaTime);
            }
        }
    }

    //Function to toggle flash light
    public void ToggleFlashLight(bool status)
    {
        flashLightStatus = status;
        flashLight.SetActive(status);
        CmdUpdateFlashLightStatus(status);
    }

    //Function to recharge flash light
    public void RechargeFlashLight(float amount)
    {
        flashLightBattery += amount;
        CmdUpdateFlashLightBattery(flashLightBattery);
    }

    //Function to drain flash light
    public void DrainFlashLight(float amount)
    {
        flashLightBattery -= amount;
        CmdUpdateFlashLightBattery(flashLightBattery);
    }

    //SETTERS
    public void SetFlashLightCharge(float amount)
    {
        flashLightBattery = amount;
        CmdUpdateFlashLightBattery(flashLightBattery);
    }
    public void SetMaxFlashLightCharge(float amount)
    {
        flashLightMaxBattery = amount;
        CmdUpdateFlashLightBattery(flashLightMaxBattery);
    }

    //HOOK SETTERS
    void HookSetStatus(bool oldStatus, bool newStatus)
    {
        flashLightStatus = newStatus;
        flashLight.SetActive(newStatus);
    }

    void HookSetBattery(float oldBattery, float newBattery)
    {
        flashLightBattery = newBattery;
    }

    void HookSetMaxBattery(float oldMaxBattery, float newMaxBattery)
    {
        flashLightMaxBattery = newMaxBattery;
    }


    //GETTERS
    public float GetFlashLightCharge()
    {
        return flashLightBattery;
    }

    public float GetMaxFlashLightCharge()
    {
        return flashLightMaxBattery;
    }

    public bool GetFlashLightStatus()
    {
        return flashLightStatus;
    }

    public float GetBaseFlashLightCharge()
    {
        return baseFlashLightBattery;
    }

    //Commands to update flashlight behaviour

    [Command]
    public void CmdUpdateFlashLightStatus(bool newStatus)
    {
        flashLightStatus = newStatus;
        flashLight.SetActive(newStatus);
    }

    [Command]
    public void CmdUpdateFlashLightBattery(float battery)
    {
        flashLightBattery = battery;
    }

    [Command]
    public void CmdUpdateMaxFlashLightBattery(float maxBattery)
    {
        flashLightMaxBattery = maxBattery;
    }
}
