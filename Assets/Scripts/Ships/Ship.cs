using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Ship : MonoBehaviour
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

    private void Awake()
    {
        InitialiseComponents();
    }

    public void Update()
    {
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
        //Add using "specific" method at out of range index
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            bool success = AddComponentToShip(testCargoContainerComponent, 3);
            Debug.Log("Added component: " + success);
        }
    }

    // Start is called before the first frame update
    void InitialiseComponents()
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

        //Get Mesh Components
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();

        //Load mesh data from prefab
        meshFilter.mesh = shipAsset.shipPrefab.transform.GetChild(0).GetComponent<MeshFilter>().sharedMesh;
        meshRenderer.materials = shipAsset.shipPrefab.transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterials;
        meshCollider.sharedMesh = shipAsset.shipPrefab.transform.GetChild(0).GetComponent<MeshCollider>().sharedMesh;

        //Expansion Slot Instantiation
        for (int i = 0; i < shipAsset.expansionComponentCount; i++)
        {
            int index = i + 1;
            GameObject slot = shipAsset.shipPrefab.transform.GetChild(index).gameObject;
            expansionComponentSlots[i] = Instantiate(slot, slot.transform.position + transform.position, slot.transform.rotation, transform);
        }

        //Large Slot Instantiation
        for (int i = 0; i < shipAsset.largeComponentCount; i++)
        {
            int index = i + shipAsset.expansionComponentCount + 1;
            GameObject slot = shipAsset.shipPrefab.transform.GetChild(index).gameObject;
            largeComponentSlots[i] = Instantiate(slot, slot.transform.position + transform.position, slot.transform.rotation, transform);
        }
    }

    //Function to add a component to a ship by filling the array linearly
    public bool AddComponentToShip(ShipComponentAsset componentToAdd)
    {
        //Get relevant aray from type
        ShipComponentAsset[] componentArray = GetComponentArrayFromComponentType(componentToAdd.componentType);

        //Check if there is available space in this array
        if(HasAvailableSpaceInComponentArray(componentArray, out int componentIndex) == true)
        {
            //Add to relevant array
            componentArray[componentIndex] = componentToAdd;
            //Was sucessfully added to array
            return true;
        }

        //Was not successful, no space in array
        return false;
    }

    //Function to add a component to a ship at a specific index
    public bool AddComponentToShip(ShipComponentAsset componentToAdd, int index)
    {
        //Get relevant aray from type
        ShipComponentAsset[] componentArray = GetComponentArrayFromComponentType(componentToAdd.componentType);

        //Check if there is available space in this array at the specified index
        if (HasAvailableSpaceAtComponentIndex(componentArray, index))
        {
            componentArray[index] = componentToAdd;
            return true;
        }

        return false;
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
}