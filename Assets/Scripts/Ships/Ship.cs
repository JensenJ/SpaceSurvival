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

    private void Awake()
    {
        InitialiseComponents();
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
}
