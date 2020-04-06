using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ShipAsset")]
public class ShipAsset : ScriptableObject
{
    public GameObject shipPrefab;
    public float maxDurability;

    [Header("Forward Movement Values:")]
    public float forwardMaxSpeed;
    public float forwardAcceleration;
    public float forwardDeceleration;
    public float forwardBrake;

    [Header("Roll Movement Values:")]
    public float rollMaxSpeed;
    public float rollAcceleration;
    public float rollDeceleration;

    //Slot counts
    [Header("Component Slot Counts:")]
    public int smallComponentCount;
    public int mediumComponentCount;
    public int largeComponentCount;
    public int expansionComponentCount;
    public int upgradeComponentCount;
    
}
