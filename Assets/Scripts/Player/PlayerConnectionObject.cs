using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerConnectionObject : NetworkBehaviour
{
    public GameObject gameManager = null;
    public GameObject playerGameObjectPrefab = null;
    public string playerName = "Player";
    public GameObject playerGameObject = null;

    public GameObject shipObjectPrefab = null;


    // Start is called before the first frame update
    void Start()
    {
        gameManager = GameObject.FindGameObjectWithTag("GameController");

        if(isLocalPlayer == false)
        {
            //This object belongs to another player
            return;
        }

        //Spawn object
        Debug.Log("PlayerObject::Start - Spawning player game object");

        CmdSpawnPlayerGameObject();
        CmdChangePlayerName("Player" + Random.Range(1, 100));

        CmdSpawnShip();
    }

    // Update is called once per frame
    void Update()
    {
        if(isLocalPlayer == false)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            CmdChangePlayerName("Player" + Random.Range(1, 100));
        }
    }

    void OnDestroy()
    {
        if(playerGameObject != null)
        {
            playerGameObject.GetComponent<PlayerController>().EnableCursor();
        }
        Destroy(playerGameObject);
    }

    //Function to update all commands and refresh all clients, useful for when a new player joins
    void Resync()
    {
        CmdChangePlayerName(playerName);

        PlayerFlashLight flashlight = playerGameObject.GetComponent<PlayerFlashLight>();
        if(flashlight != null)
        {
            CmdUpdateFlashLightStatus(flashlight.flashLightStatus);
            CmdUpdateFlashLightBattery(flashlight.GetFlashLightCharge(), flashlight.GetMaxFlashLightCharge());
        }

        PlayerAttributes attributes = playerGameObject.GetComponent<PlayerAttributes>();
        if (attributes != null)
        {
            CmdUpdatePlayerAttributes(attributes.GetHealth(), attributes.GetMaxHealth(), attributes.GetStamina(), attributes.GetMaxStamina());
        }
    }

    /////////////////////////////// COMMANDS ///////////////////////////////
    //Commands are only executed on the host client / server
    //Commands guarantee that the function is running on the server

    #region PlayerCommands
    //Command to spawn player on server
    [Command]
    void CmdSpawnPlayerGameObject()
    {
        Debug.Log("CMD: Spawning new player");
        //Creating object on server
        playerGameObject = Instantiate(playerGameObjectPrefab);

        playerGameObject.transform.position = transform.position;
        playerGameObject.transform.rotation = transform.rotation;

        //Spawn object on all clients
        NetworkServer.Spawn(playerGameObject, connectionToClient);
    }

    //Command to change player name on server
    [Command]
    public void CmdChangePlayerName(string newName)
    {
        Debug.Log("CMD: Player name change requested: " + playerName + " to " + newName);
        RpcChangePlayerName(newName);
        playerName = newName;
    }
    
    //Command to toggle flashlight
    [Command]
    public void CmdUpdateFlashLightStatus(bool flashLightStatus)
    {
        Debug.Log("CMD: Update Flash Light Status");
        PlayerFlashLight flashlight = playerGameObject.GetComponent<PlayerFlashLight>();
        if (flashlight != null)
        {
            RpcUpdateFlashLightStatus(flashLightStatus);
        }
    }

    //Command to update flashlight battery info
    [Command]
    public void CmdUpdateFlashLightBattery(float flashLightBattery, float flashLightMaxBattery)
    {
        Debug.Log("CMD: Update Flash Light Battery");
        PlayerFlashLight flashlight = playerGameObject.GetComponent<PlayerFlashLight>();
        if (flashlight != null)
        {
            RpcUpdateFlashLightBattery(flashLightBattery, flashLightMaxBattery);

            flashlight.SetFlashLightCharge(flashLightBattery);
            flashlight.SetMaxFlashLightCharge(flashLightMaxBattery);
        }
    }

    //Command to update player attributes
    [Command]
    public void CmdUpdatePlayerAttributes(float health, float maxHealth, float stamina, float maxStamina)
    {
        Debug.Log("CMD: Update Player Attributes");
        PlayerAttributes attributes = playerGameObject.GetComponent<PlayerAttributes>();
        if(attributes != null)
        {
            RpcUpdatePlayerAttributes(attributes.GetHealth(), attributes.GetMaxHealth(), attributes.GetStamina(), attributes.GetMaxStamina());

            attributes.SetHealth(health);
            attributes.SetMaxHealth(maxHealth);
            attributes.SetStamina(stamina);
            attributes.SetMaxStamina(maxStamina);
        }
    }
    #endregion

    #region ShipCommands

    [Command]
    void CmdSpawnShip()
    {
        GameObject shipObject = Instantiate(shipObjectPrefab);

        NetworkServer.Spawn(shipObject, connectionToClient);
    }
    #endregion

    /////////////////////////////// RPC ///////////////////////////////
    //RPCs (remote procedure calls) are functions that are only executed on clients

    #region PlayerRPCs

    //RPC to set the player name
    [ClientRpc]
    void RpcChangePlayerName(string newName)
    {
        //Change game object name
        gameObject.name = "PlayerConnectionObject(" + newName + ")";
        //Setting manually as when a hook is used the local value does not get updated
        playerName = newName;
        playerGameObject.name = "Player(" + newName + ")";
    }

    //RPC to set the flash light status
    [ClientRpc]
    void RpcUpdateFlashLightStatus(bool status)
    {
        PlayerFlashLight flashlight = playerGameObject.GetComponent<PlayerFlashLight>();
        if(flashlight != null)
        {
            flashlight.ToggleFlashLight(status);
        }
    }

    //RPC to update flash light battery
    [ClientRpc]
    void RpcUpdateFlashLightBattery(float flashLightBattery, float flashLightMaxBattery)
    {
        PlayerFlashLight flashlight = playerGameObject.GetComponent<PlayerFlashLight>();
        if (flashlight != null)
        {
            //Only sync for local player, prevents incorrect data being synced, fixes issue #5 on GitHub
            if (hasAuthority)
            {
                return;
            }

            flashlight.SetFlashLightCharge(flashLightBattery);
            flashlight.SetMaxFlashLightCharge(flashLightMaxBattery);
        }
    }

    //RPC to update player attributes such as health
    [ClientRpc]
    void RpcUpdatePlayerAttributes(float health, float maxHealth, float stamina, float maxStamina)
    {
        PlayerAttributes attributes = playerGameObject.GetComponent<PlayerAttributes>();
        if(attributes != null)
        {
            //Only sync for local player, prevents incorrect data being synced, fixes issue #5 on GitHub
            if (hasAuthority)
            {
                return;
            }

            attributes.SetHealth(health);
            attributes.SetMaxHealth(maxHealth);
            attributes.SetStamina(stamina);
            attributes.SetMaxStamina(maxStamina);
        }
    }
    #endregion
}
