using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Derived class for ship interaction
public class InteractableShip : Interactable
{
    protected override void OnInteract(GameObject interactingObject)
    {

        //Disable the interacting object
        interactingObject.SetActive(false);
        Debug.Log("Ship interacted");

        //TODO: add ship interact functionality
        transform.GetChild(0).gameObject.SetActive(true);
    }
}
