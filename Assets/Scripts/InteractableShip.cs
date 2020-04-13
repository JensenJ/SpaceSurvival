using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Derived class for ship interaction
public class InteractableShip : Interactable
{
    protected override void OnInteract(GameObject interactingPlayer)
    {
        //Authority check
        if (!hasAuthority)
        {
            return;
        }

        //Get ship objects
        GameObject shipCamera = transform.GetChild(0).gameObject;
        GameObject shipObject = transform.GetChild(1).gameObject;

        //Ship controller activation
        ShipController shipController = shipObject.GetComponent<ShipController>();

        //If the ship contains a player
        if(shipController.playerObject != null)
        {
            //Don't try to get in it
            return;
        }

        //Disable the interacting object
        interactingPlayer.SetActive(false);
        Debug.Log("Ship interacted");

        //Camera disabling
        shipCamera.SetActive(true);

        //Ship controller settings
        shipController.canMove = true;
        shipController.CmdSetPlayerObject(interactingPlayer);

        shipController.canExitShip = true;
    }
}
