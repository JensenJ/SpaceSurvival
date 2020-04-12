using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Derived class for ship interaction
public class InteractableShip : Interactable
{
    protected override void OnInteract(GameObject interactingPlayer)
    {
        //Disable the interacting object
        interactingPlayer.SetActive(false);
        Debug.Log("Ship interacted");

        //Get ship objects
        GameObject shipCamera = transform.GetChild(0).gameObject;
        GameObject shipObject = transform.GetChild(1).gameObject;

        //Camera disabling
        shipCamera.SetActive(true);

        //Ship controller activation
        ShipController shipController = shipObject.GetComponent<ShipController>();

        //Ship controller settings
        shipController.canMove = true;
        shipController.playerObject = interactingPlayer;

        shipController.canExitShip = true;
    }
}
