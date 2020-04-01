using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Scriptable object class to hold data specific to ship components
[CreateAssetMenu(menuName = "ShipComponentAsset")]
public class ShipComponentAsset : ScriptableObject
{
    public string componentName = "DEFAULT COMPONENT NAME";
    public string componentDescription = "DEFAULT COMPONENT DESCRIPTION";
    public float durability = 100;
    public float maxDurability = 100;
    public GameObject componentPrefab = null;
    public ShipComponentType componentType = ShipComponentType.Small;
    public float powerContribution = -10;
}

//Enum for component types
public enum ShipComponentType
{
    Small,
    Medium,
    Large,
    Expansion,
    Upgrade,
    Special
}
