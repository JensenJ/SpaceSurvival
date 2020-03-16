using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainManipulator : MonoBehaviour
{
    Camera cam;

    private void Start()
    {
        cam = transform.GetChild(0).GetComponent<Camera>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 1.0f));
            RaycastHit hit;

            if(Physics.Raycast(ray, out hit))
            {
                if(hit.transform.tag == "Terrain")
                {
                    Debug.Log("Terrain place: " + hit.point);
                    hit.transform.GetComponent<Chunk>().PlaceTerrain(hit.point);
                }
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 1.0f));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform.tag == "Terrain")
                {
                    Debug.Log("Terrain remove: " + hit.point);
                    hit.transform.GetComponent<Chunk>().RemoveTerrain(hit.point);
                }
            }
        }
    }
}
