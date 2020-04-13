using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerConnectionObject : NetworkBehaviour
{
    public GameObject gameManager = null;
    public GameObject playerGameObjectPrefab = null;
    public GameObject playerGameObject = null;

    public GameObject shipObjectPrefab = null;


    // Start is called before the first frame update
    void Start()
    {
        gameManager = GameObject.FindGameObjectWithTag("GameController");

        if (isLocalPlayer == false)
        {
            //This object belongs to another player
            return;
        }

        //Spawn object
        Debug.Log("PlayerObject::Start - Spawning player game object");

        CmdSpawnPlayerGameObject();

        CmdSpawnShip();
    }

    // Update is called once per frame
    void Update()
    {
        if (isLocalPlayer == false)
        {
            return;
        }
    }

    void OnDestroy()
    {
        if (playerGameObject != null)
        {
            playerGameObject.GetComponent<PlayerController>().EnableCursor();
        }
        Destroy(playerGameObject);
    }

    /////////////////////////////// COMMANDS ///////////////////////////////
    //Commands are only executed on the host client / server
    //Commands guarantee that the function is running on the server

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

    [Command]
    void CmdSpawnShip()
    {
        GameObject shipObject = Instantiate(shipObjectPrefab);

        NetworkServer.Spawn(shipObject, connectionToClient);
    }
}