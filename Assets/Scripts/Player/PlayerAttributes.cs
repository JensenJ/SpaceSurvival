using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerAttributes : NetworkBehaviour
{
    [SerializeField] [SyncVar(hook = nameof(HookSetHealth))] public float health = 100.0f;
    [SerializeField] [SyncVar(hook = nameof(HookSetStamina))] public float stamina = 100.0f;
    [SerializeField] [SyncVar(hook = nameof(HookSetMaxHealth))] public float maxHealth = 100.0f;
    [SerializeField] [SyncVar(hook = nameof(HookSetMaxStamina))] public float maxStamina = 100.0f;

    [SerializeField] public static float baseHealth = 100.0f;
    [SerializeField] public static float baseStamina = 100.0f;

    // Update is called once per frame
    void Update()
    {
        //Authority check
        if (!hasAuthority)
        {
            return;
        }
    }

    //Functions for changing attribute values
    public void DamageHealth(float amount)
    {
        health -= amount;
        CmdUpdateHealth(health);
    }

    public void HealHealth(float amount)
    {
        health += amount;
        CmdUpdateHealth(health);
    }

    public void DamageStamina(float amount)
    {
        stamina -= amount;
        CmdUpdateStamina(stamina);
    }

    public void HealStamina(float amount)
    {
        stamina += amount;
        CmdUpdateStamina(stamina);
    }


    //SETTERS
    public void SetHealth(float newHealth)
    {
        CmdUpdateHealth(newHealth);
    }
    public void SetMaxHealth(float newMaxHealth)
    {
        CmdUpdateMaxHealth(newMaxHealth);
    }
    public void SetStamina(float newStamina)
    {
        CmdUpdateStamina(newStamina);
    }
    public void SetMaxStamina(float newMaxStamina)
    {
        CmdUpdateMaxStamina(newMaxStamina);
    }

    //SyncVar Hook SETTERS
    void HookSetHealth(float oldHealth, float newHealth)
    {
        health = newHealth;
    }
    void HookSetMaxHealth(float oldMaxHealth, float newMaxHealth)
    {
        maxHealth = newMaxHealth;
    }
    void HookSetStamina(float oldStamina, float newStamina)
    {
        stamina = newStamina;
    }
    void HookSetMaxStamina(float oldMaxStamina, float newMaxStamina)
    {
        maxStamina = newMaxStamina;
    }

    //GETTERS
    public float GetHealth()
    {
        return health;
    }
    public float GetMaxHealth()
    {
        return maxHealth;
    }
    public float GetStamina()
    {
        return stamina;
    }
    public float GetMaxStamina()
    {
        return maxStamina;
    }
    public float GetBaseHealth()
    {
        return baseHealth;
    }
    public float GetBaseStamina()
    {
        return baseStamina;
    }

    //Command to update health
    [Command]
    public void CmdUpdateHealth(float health)
    {
        this.health = health;
    }

    //Command to update max health
    [Command]
    public void CmdUpdateMaxHealth(float maxHealth)
    {
        this.maxHealth = maxHealth;
    }

    //Command to update stamina
    [Command]
    public void CmdUpdateStamina(float stamina)
    {
        this.stamina = stamina;
    }

    //Command to update max stamina
    [Command]
    public void CmdUpdateMaxStamina(float maxStamina)
    {
        this.maxStamina = maxStamina;
    }
}