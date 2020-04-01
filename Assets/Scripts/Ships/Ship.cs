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
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            bool success = AddComponentToShip(testCargoContainerComponent);
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

    //TODO: Refactor function and make it so that actual components are spawned in world for large and expansion components
    //Function to add a component to a ship
    public bool AddComponentToShip(ShipComponentAsset componentToAdd)
    {
        ShipComponentType type = componentToAdd.componentType;
        //If type is small
        if(type == ShipComponentType.Small)
        {
            //If space available, add to array
            if (CheckComponentArrayAvailability(smallComponents, out int componentIndex) == true)
            {
                //Add to component array and return true
                smallComponents[componentIndex] = componentToAdd;
                return true;
            }
        }
        else if(type == ShipComponentType.Medium)
        {
            //If space available, add to array
            if (CheckComponentArrayAvailability(mediumComponents, out int componentIndex) == true)
            {
                //Add to component array and return true
                mediumComponents[componentIndex] = componentToAdd;
                return true;
            }
        }
        else if (type == ShipComponentType.Large)
        {
            //If space available, add to array
            if (CheckComponentArrayAvailability(largeComponents, out int componentIndex) == true)
            {
                //Add to component array and return true
                largeComponents[componentIndex] = componentToAdd;
                return true;
            }
        }
        else if (type == ShipComponentType.Expansion)
        {
            //If space available, add to array
            if (CheckComponentArrayAvailability(expansionComponents, out int componentIndex) == true)
            {
                //Add to component array and return true
                expansionComponents[componentIndex] = componentToAdd;
                return true;
            }
        }
        else if (type == ShipComponentType.Upgrade) {
            //If space available, add to array
            if (CheckComponentArrayAvailability(upgradeComponents, out int componentIndex) == true)
            {
                //Add to component array and return true
                upgradeComponents[componentIndex] = componentToAdd;
                return true;
            }
        }

        return false;
    }

    //Function to check for a space in the ship component list.
    public bool CheckComponentArrayAvailability(ShipComponentAsset[] componentArrayToCheck, out int index)
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
}
