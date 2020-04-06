using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ShipAsset")]
public class ShipAsset : ScriptableObject
{
    public GameObject shipPrefab;
    public float maxDurability;
    public float maxSpeed;
    public float acceleration;
    public float deceleration;
    public float brake;
    public float turnRate;
    //Slot counts
    public int smallComponentCount;
    public int mediumComponentCount;
    public int largeComponentCount;
    public int expansionComponentCount;
    public int upgradeComponentCount;
    
}
