using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ShipComponent : NetworkBehaviour
{
    //Network ID of the parent component Script
    [SyncVar]
    public uint parentNetID;

    //Which child index this component is under
    [SyncVar]
    public int componentSlotIndex;

    //When the client starts
    public override void OnStartClient()
    {
        //Base functionality
        base.OnStartClient();

        //Setting the correct transform for newly joined players.
        NetworkIdentity shipObject = NetworkIdentity.spawned[parentNetID];

        GameObject parentObject = shipObject.transform.GetChild(0).GetChild(componentSlotIndex).gameObject;

        transform.SetParent(parentObject.transform);
        transform.position = parentObject.transform.position;
        transform.rotation = parentObject.transform.rotation;
        transform.localScale = parentObject.transform.localScale;
    }
}
