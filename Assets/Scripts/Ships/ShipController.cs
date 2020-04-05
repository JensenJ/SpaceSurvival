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
    Ship ship;

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
    }
}
