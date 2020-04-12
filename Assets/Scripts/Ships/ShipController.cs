﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(NetworkIdentity))]
public class ShipController : NetworkBehaviour
{
    //Network ID of the parent Ship Script
    [SyncVar]
    public uint parentNetID;

    //Which ship to spawn from the index
    [SyncVar]
    public int shipSpawnIndex;

    //Parent ship class
    public Ship ship;

    //Ship forward movement variables
    float forwardMaxThrust;
    float forwardAcceleration;
    float forwardDeceleration;
    float forwardBrake;
    float forwardVelocity;

    //Ship roll movement variables
    float rollMaxSpeed;
    float rollAcceleration;
    float rollDeceleration;
    float rollVelocity;

    public bool canMove = false;
    public GameObject playerObject = null;

    float timeOfExitAttempt = float.MaxValue;
    float exitTime = 0.5f;
    public bool canExitShip;

    //When the client starts
    public override void OnStartClient()
    {
        //Base functionality
        base.OnStartClient();

        //Setting the correct transform for newly joined players. This function only updates the transform, and such with the new connecting players.
        NetworkIdentity parentObject = NetworkIdentity.spawned[parentNetID];
        transform.SetParent(parentObject.transform);
        transform.position = parentObject.transform.position;
        transform.rotation = parentObject.transform.rotation;
        transform.localScale = parentObject.transform.localScale;

        //Initialise Components, camera and ship spawn index
        ship = parentObject.GetComponent<Ship>();
        ship.shipAsset = ship.shipAssets[shipSpawnIndex];
        ship.InitialiseShipCamera();
        ship.InitialiseComponents(gameObject);

        //Forward Movement variable setting
        forwardMaxThrust = ship.shipAsset.forwardMaxSpeed;
        forwardAcceleration = ship.shipAsset.forwardAcceleration;
        forwardDeceleration = ship.shipAsset.forwardDeceleration;
        forwardBrake = ship.shipAsset.forwardBrake;

        //Roll movement variable setting
        rollMaxSpeed = ship.shipAsset.rollMaxSpeed;
        rollAcceleration = ship.shipAsset.rollAcceleration;
        rollDeceleration = ship.shipAsset.rollDeceleration;
    }

    //Function to check if the player tried to leave the ship
    public void CheckForExit()
    {
        //Check if the ship is not safe to exit or if the ship cannot move
        if (!canExitShip)
        {
            return;
        }

        //If exit key is pressed
        if (Input.GetKeyDown(KeyCode.F))
        {
            //Mark start exit time
            timeOfExitAttempt = Time.time;
        }

        if (Input.GetKey(KeyCode.F))
        {
            //Calculate time since exit started
            float timeSinceExitStart = Time.time - timeOfExitAttempt;

            //If player has been trying to exit long enough
            if (timeSinceExitStart >= exitTime)
            {
                //Exit the ship
                if (playerObject != null)
                {
                    //Disable ship functionality
                    canMove = false;
                    ship.shipCamera.SetActive(false);

                    //Enable player functionality
                    playerObject.transform.position = ship.transform.GetChild(1).GetChild(0).position;
                    playerObject.SetActive(true);
                    playerObject = null;

                    //Prevent feedback loop of exiting ship
                    canExitShip = false;
                    timeOfExitAttempt = float.MaxValue;
                }
            }
        }
    }

    public void Update()
    {
        //Is this ship owned by the player
        if (!hasAuthority)
        {
            return;
        }

        //Ship null check
        if (ship == null)
        {
            return;
        }

        //Check if the player tried to leave the ship
        CheckForExit();

        //Forward backward motion
        //Forward acceleration
        if (Input.GetKey(KeyCode.W))
        {
            if (!canMove)
            {
                return;
            }
            forwardVelocity += forwardAcceleration * Time.deltaTime;
            forwardVelocity = Mathf.Min(forwardVelocity, forwardMaxThrust);
        } 
        //Braking / backward deceleration
        else if (Input.GetKey(KeyCode.S))
        {
            if (!canMove)
            {
                return;
            }
            forwardVelocity += -forwardBrake * Time.deltaTime;
            forwardVelocity = Mathf.Max(forwardVelocity, 0);
        }
        //Deceleration without braking
        else
        {
            forwardVelocity += -forwardDeceleration * Time.deltaTime;
            forwardVelocity = Mathf.Max(forwardVelocity, 0);
        }

        //Apply forward movement
        ship.transform.position -= transform.forward * forwardVelocity * Time.deltaTime;

        //Left roll
        if (Input.GetKey(KeyCode.Q))
        {
            if (!canMove)
            {
                return;
            }
            rollVelocity += rollAcceleration * Time.deltaTime;
            rollVelocity = Mathf.Min(rollVelocity, rollMaxSpeed);
        }
        //Right roll
        else if (Input.GetKey(KeyCode.E))
        {
            if (!canMove)
            {
                return;
            }
            rollVelocity += -rollAcceleration * Time.deltaTime;
            rollVelocity = Mathf.Max(rollVelocity, -rollMaxSpeed);
        }
        //If roll velocity is negative
        else if(rollVelocity < 0)
        {
            rollVelocity += rollDeceleration * Time.deltaTime;
            rollVelocity = Mathf.Min(rollVelocity, 0);
        }
        //If roll velocity is positive
        else if(rollVelocity > 0)
        {
            rollVelocity += -rollDeceleration * Time.deltaTime;
            rollVelocity = Mathf.Max(rollVelocity, 0);
        }

        //Don't bother rotating if rotate velocity is 0
        if (rollVelocity != 0)
        {
            //Apply rotate / roll
            ship.transform.eulerAngles = new Vector3(ship.transform.eulerAngles.x, ship.transform.eulerAngles.y, ship.transform.eulerAngles.z - Time.deltaTime * rollVelocity);
        }
    }
}
