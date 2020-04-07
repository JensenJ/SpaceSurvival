using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//A base class to offer functionality for all interactables
public abstract class Interactable : MonoBehaviour
{
    //Bool for interaction permission
    public bool canInteract = true;
    protected abstract void OnInteract();
    
    //Public function for interaction with interaction check
    public void Interact()
    {
        //Check for interaction permission
        if (canInteract)
        {
            //Then interact if permission present
            OnInteract();
        }
    }

    //Function to set the interaction status. 
    //TODO: Network this function using commands / RPCs
    public void SetInteractStatus(bool interactStatus)
    {
        canInteract = interactStatus;
    }
}
