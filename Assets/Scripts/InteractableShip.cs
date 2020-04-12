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

        GameObject shipCamera = transform.GetChild(0).gameObject;
        GameObject shipObject = transform.GetChild(1).gameObject;

        shipCamera.SetActive(true);
        shipObject.GetComponent<ShipController>().canMove = true;
        

    }
}
