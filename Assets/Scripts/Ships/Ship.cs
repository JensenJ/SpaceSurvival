using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Ship : NetworkBehaviour
{
    //Object variables
    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    MeshCollider meshCollider;

    //Ship variables
    public ShipAsset shipAsset;
    public ShipComponentAsset[] smallComponents;
    public ShipComponentAsset[] mediumComponents;
    public ShipComponentAsset[] largeComponents;
    public ShipComponentAsset[] expansionComponents;
    public ShipComponentAsset[] upgradeComponents;

    public GameObject[] largeComponentSlots;
    public GameObject[] expansionComponentSlots;

    public ShipComponentAsset testCargoContainerComponent;

    public GameObject shipObject;

    public bool hasSpawnedShip = false;

    public void Update()
    {

        if(hasAuthority == false)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            if (!hasSpawnedShip)
            {
                CmdSpawnPlayerShip();
                hasSpawnedShip = true;
            }
        }

        //Add using "fill" method
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            bool success = AddComponentToShip(testCargoContainerComponent);
            Debug.Log("Added component: " + success);
        }
        //Add using "specific" method at first index
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            bool success = AddComponentToShip(testCargoContainerComponent, 0);
            Debug.Log("Added component: " + success);
        }
        //Add using "specific" method at second index
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            bool success = AddComponentToShip(testCargoContainerComponent, 1);
            Debug.Log("Added component: " + success);
        }


        //Remove at first index
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            if (RemoveComponentFromShip(ShipComponentType.Expansion, 0, out ShipComponentAsset removedComponent))
            {
                Debug.Log("Removed component " + removedComponent.componentName);
            }
            else
            {
                Debug.Log("Could not remove component");
            }
        }
        //Remove at second index
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            if (RemoveComponentFromShip(ShipComponentType.Expansion, 1, out ShipComponentAsset removedComponent))
            {
                Debug.Log("Removed component " + removedComponent.componentName);
            }
            else
            {
                Debug.Log("Could not remove component");
            }
        }
    }

    //Function to add a component to a ship by filling the array linearly
    public bool AddComponentToShip(ShipComponentAsset componentToAdd)
    {
        //Null check
        if (componentToAdd == null)
        {
            return false;
        }

        //Get relevant aray from type
        ShipComponentAsset[] componentArray = GetComponentArrayFromComponentType(componentToAdd.componentType);

        //Check if there is available space in this array
        if(HasAvailableSpaceInComponentArray(componentArray, out int componentIndex) == true)
        {
            //Add to relevant array
            componentArray[componentIndex] = componentToAdd;

            InstantiateComponent(componentToAdd, componentIndex);

            //Was sucessfully added to array
            return true;
        }

        //Was not successful, no space in array
        return false;
    }

    //Function to add a component to a ship at a specific index
    public bool AddComponentToShip(ShipComponentAsset componentToAdd, int index)
    {
        //Null check
        if (componentToAdd == null)
        {
            return false;
        }

        //Get relevant aray from type
        ShipComponentAsset[] componentArray = GetComponentArrayFromComponentType(componentToAdd.componentType);

        //Check if there is available space in this array at the specified index
        if (HasAvailableSpaceAtComponentIndex(componentArray, index))
        {
            componentArray[index] = componentToAdd;

            InstantiateComponent(componentToAdd, index);

            return true;
        }

        return false;
    }


    //Function to remove a component from a specific index of a component type from the ship
    public bool RemoveComponentFromShip(ShipComponentType componentType, int indexToRemove, out ShipComponentAsset removedComponent)
    {
        //Get the component array from the component type
        ShipComponentAsset[] componentArray = GetComponentArrayFromComponentType(componentType);

        //Check if index is not out of bounds
        if(indexToRemove > componentArray.Length - 1 || indexToRemove < 0)
        {
            //Removal was not successful as it was outside the bounds of the array and would cause an error.
            removedComponent = null;
            return false;
        }

        //Get old component before it is removed from array
        removedComponent = componentArray[indexToRemove];
        //If nothing was at the index
        if(removedComponent == null)
        {
            //Removal was not successful as there was nothing to remove
            return false;
        }

        //Set the new index to nothing / null
        componentArray[indexToRemove] = null;

        //Attempt to destroy component in world
        DestroyComponent(removedComponent, indexToRemove);
        return true;
    }

    //Function to instantiate a component gameobject in the world. 
    public void InstantiateComponent(ShipComponentAsset componentToInstantiate, int additionIndex)
    {
        //Null check
        if (componentToInstantiate == null)
        {
            return;
        }

        GameObject componentPrefab = componentToInstantiate.componentPrefab;

        //Null check
        if(componentPrefab == null)
        {
            return;
        }

        //Get slots for this type of component
        GameObject[] slots = GetSlotArrayFromComponentType(componentToInstantiate.componentType);
        //Check if slots is null, if not expansion or large component basically
        if(slots == null)
        {
            return;
        }

        //Get the slot from addition index
        GameObject slot = slots[additionIndex];
        //Spawn game object at correct position, rotation and set the parent as relative slot.
        GameObject component = Instantiate(componentPrefab, slot.transform.position, slot.transform.rotation);
        component.transform.SetParent(slot.transform);
    }

    //Function to destroy a component in the world physically attached to the ship
    public void DestroyComponent(ShipComponentAsset componentToDestroy, int removalIndex)
    {
        //Null check
        if(componentToDestroy == null)
        {
            return;
        }

        //Get slots for this type of component
        GameObject[] slots = GetSlotArrayFromComponentType(componentToDestroy.componentType);
        //Check if slots is null, if not expansion or large component basically
        if (slots == null)
        {
            return;
        }

        GameObject slot = slots[removalIndex];
        Destroy(slot.transform.GetChild(0).gameObject);
    }

    //Function to check for a space in the ship component list.
    public bool HasAvailableSpaceInComponentArray(ShipComponentAsset[] componentArrayToCheck, out int index)
    {
        //For every item in component list
        for (int i = 0; i < componentArrayToCheck.Length; i++)
        {
            //Check if there is space, aka is not assigned
            if(componentArrayToCheck[i] == null)
            {
                //Set out parameter to current index
                index = i;
                //Return
                return true;
            }
        }
        //Return after setting index to 0
        index = 0;
        return false;
    }

    //Function to check an array at a specific index for whether it has a component or not.
    public bool HasAvailableSpaceAtComponentIndex(ShipComponentAsset[] componentArrayToCheck, int indexToCheck)
    {
        //Check for out of bounds exception
        if(indexToCheck > componentArrayToCheck.Length - 1 || indexToCheck < 0)
        {
            return false;
        }

        //Check if component in array at the index is null, not assigned
        if(componentArrayToCheck[indexToCheck] == null)
        {
            //Has space at this index in the array
            return true;
        }
        else
        {
            //Does not have space at this index in the array
            return false;
        }
    }

    //Function to get the component array relevant to the component type passed in to the function.
    public ShipComponentAsset[] GetComponentArrayFromComponentType(ShipComponentType type)
    {
        if(type == ShipComponentType.Small)
        {
            return smallComponents;
        }
        else if(type == ShipComponentType.Medium)
        {
            return mediumComponents;
        }
        else if (type == ShipComponentType.Large)
        {
            return largeComponents;
        }
        else if(type == ShipComponentType.Expansion)
        {
            return expansionComponents;
        }
        else if(type == ShipComponentType.Upgrade)
        {
            return upgradeComponents;
        }
        else
        {
            return null;
        }
    }

    //Function to get the gameobject slot array from the component type
    public GameObject[] GetSlotArrayFromComponentType(ShipComponentType type)
    {
        if(type == ShipComponentType.Large)
        {
            return largeComponentSlots;
        }
        else if(type == ShipComponentType.Expansion)
        {
            return expansionComponentSlots;
        }
        else
        {
            return null;
        }
    }

    /////////////////////////////// COMMANDS ///////////////////////////////
    //Commands are only executed on the host client / server
    //Commands guarantee that the function is running on the server

    //Command to spawn a player ship correctly and all its components
    [Command]
    void CmdSpawnPlayerShip()
    {
        //Cancel if ship asset is null
        if (shipAsset == null)
        {
            return;
        }
        //Initialise Component Arrays
        smallComponents = new ShipComponentAsset[shipAsset.smallComponentCount];
        mediumComponents = new ShipComponentAsset[shipAsset.mediumComponentCount];
        largeComponents = new ShipComponentAsset[shipAsset.largeComponentCount];
        expansionComponents = new ShipComponentAsset[shipAsset.expansionComponentCount];
        upgradeComponents = new ShipComponentAsset[shipAsset.upgradeComponentCount];

        //Gameobject arrays for physical slots on ship
        largeComponentSlots = new GameObject[shipAsset.largeComponentCount];
        expansionComponentSlots = new GameObject[shipAsset.expansionComponentCount];

        //Instantiate object for network spawning
        shipObject = Instantiate(shipAsset.shipPrefab, transform.position, transform.rotation, transform);


        //Expansion Slot Instantiation
        for (int i = 0; i < shipAsset.expansionComponentCount; i++)
        {
            //Calculate index
            int index = i + 1;
            //Get slot from shipObject
            GameObject slot = shipObject.transform.GetChild(index).gameObject;
            //Assign slot
            expansionComponentSlots[i] = slot;
        }

        //Large Slot Instantiation
        for (int i = 0; i < shipAsset.largeComponentCount; i++)
        {
            //Calculate index
            int index = i + shipAsset.expansionComponentCount + 1;
            //Get slot from shipObject
            GameObject slot = shipObject.transform.GetChild(index).gameObject;
            //Assign slot
            largeComponentSlots[i] = slot;
        }

        //Set the ship controller's parent id
        ShipController shipController = shipObject.GetComponent<ShipController>();
        shipController.parentNetID = GetComponent<NetworkIdentity>().netId;

        //Spawn ship on all clients / server
        NetworkServer.Spawn(shipObject, connectionToClient);


        //Set the ship spawn data on all clients
        RpcSetShipSpawnData(shipObject);
    }

    /////////////////////////////// RPC ///////////////////////////////
    //RPCs (remote procedure calls) are functions that are only executed on clients


    //RPC to set data for a new ship spawn on clients
    [ClientRpc]
    void RpcSetShipSpawnData(GameObject ship)
    {
        ship.transform.parent = transform;
        ship.transform.position = transform.position;
        ship.transform.rotation = transform.rotation;
    }
}