using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Ship : NetworkBehaviour
{
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
    public GameObject shipCamera;

    public List<ShipAsset> shipAssets;
    public List<ShipComponentAsset> componentAssets;

    public bool hasSpawnedShip = false;

    public void Update()
    {

        if (hasAuthority == false)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            if (hasSpawnedShip)
            {
                CmdDestroyPlayerShip();
                hasSpawnedShip = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            if (!hasSpawnedShip)
            {
                CmdSpawnPlayerShip(0);
                hasSpawnedShip = true;
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            if (!hasSpawnedShip)
            {
                CmdSpawnPlayerShip(1);
                hasSpawnedShip = true;
            }
        }

        //Add component command test
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            CmdAddShipComponent(0, 0);
        }
        //Remove component at position 0 in array
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            CmdRemoveComponent(0, ShipComponentType.Expansion);
        }
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

    //Function to initialise components
    public void InitialiseComponents(GameObject shipObject)
    {
        //Initialise Component Arrays
        smallComponents = new ShipComponentAsset[shipAsset.smallComponentCount];
        mediumComponents = new ShipComponentAsset[shipAsset.mediumComponentCount];
        largeComponents = new ShipComponentAsset[shipAsset.largeComponentCount];
        expansionComponents = new ShipComponentAsset[shipAsset.expansionComponentCount];
        upgradeComponents = new ShipComponentAsset[shipAsset.upgradeComponentCount];

        //Gameobject arrays for physical slots on ship
        largeComponentSlots = new GameObject[shipAsset.largeComponentCount];
        expansionComponentSlots = new GameObject[shipAsset.expansionComponentCount];

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
    }

    /////////////////////////////// COMMANDS ///////////////////////////////
    //Commands are only executed on the host client / server
    //Commands guarantee that the function is running on the server

    #region Commands
        
    //Command to spawn a player ship correctly and all its components
    [Command]
    void CmdSpawnPlayerShip(int shipSpawnIndex)
    {
        shipAsset = shipAssets[shipSpawnIndex];

        //Cancel if ship asset is null
        if (shipAsset == null)
        {
            return;
        }

        //Instantiate object for network spawning
        shipObject = Instantiate(shipAsset.shipPrefab, transform.position, transform.rotation, transform);

        //Get camera
        shipCamera = transform.GetChild(0).gameObject;
        //Rotate camera
        shipCamera.transform.Rotate(new Vector3(10, 180, 0));
        //Check camera distance is not negative or zero, which would cause a division by zero error.
        if(shipAsset.cameraDistance <= 0)
        {
            shipCamera.transform.position = new Vector3(0, 0, 0) + transform.position;
        }
        else
        {
            shipCamera.transform.position = new Vector3(0, shipAsset.cameraDistance / 3, -shipAsset.cameraDistance) + transform.position;
        }

        //Init components
        InitialiseComponents(shipObject);

        //Set the ship controller's parent id and spawn index for newly connecting clients
        ShipController shipController = shipObject.GetComponent<ShipController>();
        shipController.parentNetID = GetComponent<NetworkIdentity>().netId;
        shipController.shipSpawnIndex = shipSpawnIndex;

        //Spawn ship on all clients / server
        NetworkServer.Spawn(shipObject, connectionToClient);

        //Set the ship spawn data on all clients
        RpcSetShipSpawnData(shipObject, shipSpawnIndex);
    }

    //Command to destroy a player ship
    [Command]
    void CmdDestroyPlayerShip()
    {
        //Null check
        if (shipObject == null)
        {
            return;
        }

        //Destroy ship object across the network
        NetworkServer.Destroy(shipObject);
    }

    //Command to add a component to a ship
    [Command]
    void CmdAddShipComponent(int componentSpawnIndex, int componentSlotIndex)
    {
        ShipComponentAsset componentToAdd = componentAssets[componentSpawnIndex];

        //Null check
        if (componentToAdd == null)
        {
            return;
        }

        //Get relevant aray from type
        ShipComponentAsset[] componentArray = GetComponentArrayFromComponentType(componentToAdd.componentType);

        //Check if there is available space in this array at the specified index
        if (HasAvailableSpaceAtComponentIndex(componentArray, componentSlotIndex))
        {
            //Assign in component array
            componentArray[componentSlotIndex] = componentToAdd;

            GameObject component = null;

            if (componentToAdd.componentType == ShipComponentType.Expansion || componentToAdd.componentType == ShipComponentType.Large) {
                GameObject componentPrefab = componentToAdd.componentPrefab;

                //Null check
                if (componentPrefab == null)
                {
                    return;
                }

                //Get slots for this type of component
                GameObject[] slots = GetSlotArrayFromComponentType(componentToAdd.componentType);
                //Check if slots is null, if not expansion or large component basically
                if (slots == null)
                {
                    return;
                }

                //Get the slot from addition index
                GameObject slot = slots[componentSlotIndex];
                //Spawn game object at correct position, rotation and set the parent as relative slot.
                component = Instantiate(componentPrefab, slot.transform.position, slot.transform.rotation);
                component.transform.SetParent(slot.transform);

                //Set the components's parent object (the slot)
                ShipComponent shipComponent = component.GetComponent<ShipComponent>();
                shipComponent.parentNetID = GetComponent<NetworkIdentity>().netId;

                //Setting index for large components
                if (componentToAdd.componentType == ShipComponentType.Large)
                {
                    shipComponent.componentSlotIndex = 1 + componentSlotIndex + expansionComponentSlots.Length;
                }

                //Setting index for expansion components
                if (componentToAdd.componentType == ShipComponentType.Expansion)
                {
                    shipComponent.componentSlotIndex = 1 + componentSlotIndex;
                }

                //Spawn on server
                NetworkServer.Spawn(component, connectionToClient);

            }
            //Add component on clients
            RpcAddComponent(componentSlotIndex, componentSpawnIndex, component);
        }
    }

    //A command to remove a component from a ship at the specified index within the specified slot array
    [Command]
    void CmdRemoveComponent(int componentSlotIndex, ShipComponentType shipComponentType)
    {
        //Remove from array
        ShipComponentAsset[] componentArray = GetComponentArrayFromComponentType(shipComponentType);
        componentArray[componentSlotIndex] = null;

        //Try to get physical slots
        GameObject[] slots = GetSlotArrayFromComponentType(shipComponentType);
        //Null check
        if(slots == null)
        {
            return;
        }

        //Get the slot
        GameObject slot = slots[componentSlotIndex];
        //Null check
        if (slot == null)
        {
            return;
        }

        //Check whether the slot has any children (components) attached
        if(slot.transform.childCount == 0)
        {
            return;
        }

        //Get component and destroy it
        GameObject component = slot.transform.GetChild(0).gameObject;
        NetworkServer.Destroy(component);
    }

    #endregion

    /////////////////////////////// RPC ///////////////////////////////
    //RPCs (remote procedure calls) are functions that are only executed on clients

    //RPC to set data for a new ship spawn on clients
    [ClientRpc]
    void RpcSetShipSpawnData(GameObject ship, int shipSpawnIndex)
    {
        //Assign ship spawn index
        shipAsset = shipAssets[shipSpawnIndex];

        //Init componenents
        InitialiseComponents(ship);

        //Set ship position etc.
        ship.transform.parent = transform;
        ship.transform.position = transform.position;
        ship.transform.rotation = transform.rotation;
    }

    //RPC to set data for newly spawned components on ships
    [ClientRpc]
    void RpcAddComponent(int componentSlotIndex, int componentSpawnIndex, GameObject component)
    {
        //Get component
        ShipComponentAsset componentToAdd = componentAssets[componentSpawnIndex];

        //Get componentArray type from component type
        ShipComponentAsset[] componentArray = GetComponentArrayFromComponentType(componentToAdd.componentType);

        //Assign in component array
        componentArray[componentSlotIndex] = componentAssets[componentSpawnIndex];

        //If component type is large or expansion, has a presence in the world as an object
        if (componentToAdd.componentType == ShipComponentType.Expansion || componentToAdd.componentType == ShipComponentType.Large)
        {
            //Null check for component
            if(component == null)
            {
                return;
            }

            //Get slots for this type of component
            GameObject[] slots = GetSlotArrayFromComponentType(componentToAdd.componentType);
            //Check if slots is null, if not expansion or large component basically
            if (slots == null)
            {
                return;
            }

            //Get the slot from addition index
            GameObject slot = slots[componentSlotIndex];

            //Set transform
            component.transform.parent = slot.transform;
            component.transform.position = slot.transform.position;
            component.transform.rotation = slot.transform.rotation;
        }
    }
}