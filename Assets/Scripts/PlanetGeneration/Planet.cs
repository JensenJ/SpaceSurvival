using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planet : MonoBehaviour
{
    [SerializeField, HideInInspector]
    MeshFilter[] meshFilters;
    TerrainFace[] terrainFaces;

    public Transform player;
    public float distanceToPlayer;

    public int startResolution = 9;
    public float cullingMinAngle = 1.91986218f;
    public float size = 1000;

    //Hardcoded detail levels
    public float[] detailLevelDistances = new float[]
    {
        Mathf.Infinity,
        6000f,
        2500f,
        1000f,
        400f,
        150f,
        70f,
        30f,
        10f
    };

    private void Start()
    {
        //Set player controller
        player = GameObject.FindGameObjectWithTag("Player").transform;

        Initialise();
        GenerateMesh();

        StartCoroutine(PlanetGenerationLoop());
    }

    private void Update()
    {
        distanceToPlayer = Vector3.Distance(transform.position, player.position);
    }

    private IEnumerator PlanetGenerationLoop()
    {
        GenerateMesh();
        while (true)
        {
            yield return new WaitForSeconds(0.1f);
            UpdateMesh();
        }
    }

    void Initialise()
    {
        if(meshFilters == null || meshFilters.Length == 0)
        {
            meshFilters = new MeshFilter[6];

        }

        terrainFaces = new TerrainFace[6];

        Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

        for (int i = 0; i < 6; i++)
        {
            if (meshFilters[i] == null)
            {
                GameObject meshObj = new GameObject("Mesh");
                meshObj.transform.parent = transform;

                meshFilters[i] = meshObj.AddComponent<MeshFilter>();
                meshFilters[i].sharedMesh = new Mesh();
                meshObj.AddComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("HDRP/Lit"));
            }

            terrainFaces[i] = new TerrainFace(meshFilters[i].sharedMesh, startResolution, directions[i], size, this);
        }
    }

    //Function to generate the mesh
    void GenerateMesh()
    {
        foreach(TerrainFace face in terrainFaces)
        {
            face.ConstructTree();
        }
    }

    //Function to update the mesh
    void UpdateMesh()
    {
        foreach (TerrainFace face in terrainFaces)
        {
            face.UpdateTree();
        }
    }
}
