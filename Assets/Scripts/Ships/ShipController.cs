using System.Collections;
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

    //Ship movement variables
    float maxThrust;
    float turnRate;
    float acceleration;
    float deceleration;
    float brake;
    float forwardVelocity;

    Rigidbody rb;

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

        //Initialise Components and ship spawn index
        ship = parentObject.GetComponent<Ship>();
        ship.shipAsset = ship.shipAssets[shipSpawnIndex];
        ship.InitialiseComponents(gameObject);

        //Movement variable setting
        turnRate = ship.shipAsset.turnRate;
        maxThrust = ship.shipAsset.maxSpeed;
        acceleration = ship.shipAsset.acceleration;
        deceleration = ship.shipAsset.deceleration;
        brake = ship.shipAsset.brake;
        rb = ship.GetComponent<Rigidbody>();
        rb.isKinematic = true;

        Debug.Log("Acceleration: " + acceleration); 
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

        //Forward backward motion
        //Forward acceleration
        if (Input.GetKey(KeyCode.UpArrow))
        {
            forwardVelocity += acceleration * Time.deltaTime;
            forwardVelocity = Mathf.Min(forwardVelocity, maxThrust);
        } 
        //Braking / backward deceleration
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            forwardVelocity += -brake * Time.deltaTime;
            forwardVelocity = Mathf.Max(forwardVelocity, 0);
        }
        //Deceleration without braking
        else
        {
            forwardVelocity += -deceleration * Time.deltaTime;
            forwardVelocity = Mathf.Max(forwardVelocity, 0);
        }

        ship.transform.Translate(transform.forward * forwardVelocity * Time.deltaTime);
    }
}
