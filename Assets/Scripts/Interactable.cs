using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//A base class to offer functionality for all interactables
public abstract class Interactable : MonoBehaviour
{
    //Bool for interaction permission
    public bool canInteract = true;
    protected abstract void OnInteract(GameObject interactingObject);
    
    //Public function for interaction with interaction check
    public void Interact(GameObject interactingObject)
    {
        //Check for interaction permission
        if (canInteract && interactingObject != null)
        {
            //Then interact if permission present
            OnInteract(interactingObject);
        }
    }

    //Function to set the interaction status.
    public void SetInteractStatus(bool interactStatus)
    {
        canInteract = interactStatus;
    }
}
