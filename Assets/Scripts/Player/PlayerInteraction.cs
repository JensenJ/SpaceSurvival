using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerInteraction : NetworkBehaviour
{
    [SerializeField] float interactRange = 5.0f;
    [SerializeField] GameObject cam;

    float timeOfLastInteraction;
    Interactable lastInteractable;

    private void Start()
    {
        //Authority check
        if (!hasAuthority)
        {
            //Try to get camera if its not been assigned
            if (cam == null)
            {
                cam = transform.GetChild(0).gameObject;
            }
        }
    }

    private void Update()
    {
        //Authority check
        if (!hasAuthority)
        {
            return;
        }

        //If player presses F (interact key)
        if (Input.GetKeyDown(KeyCode.F))
        {
            //If object in front of player within interact range
            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, interactRange))
            {
                //Get the interactable object
                GameObject hitObject = hit.transform.gameObject;
                lastInteractable = TraceInteractWithParents(hitObject);
                timeOfLastInteraction = Time.time;
            }
        }

        //Check interactable is not null
        if(lastInteractable == null)
        {
            return;
        }

        //Check the object can actually be interacted with
        if (!lastInteractable.canInteract)
        {
            return;
        }

        //If F is still held down
        if (Input.GetKey(KeyCode.F))
        {
            float timeSinceInteractBegan = Time.time - timeOfLastInteraction;
            //If has been interacting for object for enough time
            if(timeSinceInteractBegan >= lastInteractable.interactionTime)
            {
                //Invoke interact function on the interactable
                lastInteractable.Interact(transform.gameObject);
                //Prevent feedback loop of interacting
                timeOfLastInteraction = float.MaxValue;
            }
        }
    }

    //Function to trace up the hierarchy to find all potential interaction points on a mesh. Used when objects are made up of many colliders so the parent would have the interactable
    private Interactable TraceInteractWithParents(GameObject interactObject)
    {
        bool hasFoundInteractable = false;
        GameObject currentObject = interactObject;
        //While interactable not found
        while(hasFoundInteractable == false)
        {
            //Try get interactable
            Interactable interactable = currentObject.GetComponent<Interactable>();

            //Check if object has interactable script
            if (interactable != null)
            {
                return interactable;
            }

            //If the current object has a gameobject parent
            if(currentObject.transform.parent != null)
            {
                //Set the current object to its parent and try find the interactable by looping again
                currentObject = currentObject.transform.parent.gameObject;
            }
            else
            {
                //It has no parent, therefore there is no interactable in this trace
                return null;
            }
        }
        return null;
    }
}
