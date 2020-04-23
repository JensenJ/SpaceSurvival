using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ShipAsset")]
public class ShipAsset : ScriptableObject
{
    public GameObject shipPrefab;
    public float maxDurability;
    public float cameraDistance;

    [Header("Thruster Particle Settins")]
    public Texture2D particleSpawnMap;
    public int maxThrustParticleSpawnRate;

    [Header("Forward Movement Values:")]
    public float forwardMaxSpeed;
    public float forwardAcceleration;
    public float forwardBrake;

    [Header("Roll Movement Values:")]
    public float rollMaxSpeed;
    public float rollAcceleration;
    public float rollDeceleration;

    [Header("Pitch Movement Values:")]
    public float pitchMaxSpeed;
    public float pitchAcceleration;
    public float pitchDeceleration;

    [Header("Yaw Movement Values:")]
    public float yawMaxSpeed;
    public float yawAcceleration;
    public float yawDeceleration;
    //Slot counts
    [Header("Component Slot Counts:")]
    public int smallComponentCount;
    public int mediumComponentCount;
    public int largeComponentCount;
    public int expansionComponentCount;
    public int upgradeComponentCount;
    
}
