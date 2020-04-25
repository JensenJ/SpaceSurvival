using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planet : MonoBehaviour
{
    [SerializeField, HideInInspector]
    MeshFilter[] meshFilters;
    TerrainFace[] terrainFaces;

    public Transform player;

    [HideInInspector]
    public float distanceToPlayer;

    [HideInInspector]
    public float distanceToPlayerPow2;

    public float cullingMinAngle = 1.45f;
    public float size = 1000;

    //Hardcoded base detail levels
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

    public bool devMode = false;

    public PlanetNoiseFilter noiseFilter;

    private void Awake()
    {
        //Set player controller
        player = GameObject.FindGameObjectWithTag("Player").transform;
        distanceToPlayer = Vector3.Distance(transform.position, player.position);
    }

    private void Start()
    {
        float detailLevelMultiplier = size / 1000;
        //For every detail level distances except Mathf.Infinity at index 0
        for (int i = 1; i < detailLevelDistances.Length; i++)
        {
            detailLevelDistances[i] = detailLevelMultiplier * detailLevelDistances[i];
        }

        Initialise();
        GenerateMesh();


        StartCoroutine(PlanetGenerationLoop());
    }

    private void Update()
    {
        distanceToPlayer = Vector3.Distance(transform.position, player.position);
        distanceToPlayerPow2 = distanceToPlayer * distanceToPlayer;
    }

    private IEnumerator PlanetGenerationLoop()
    {
        GenerateMesh();
        while (true)
        {
            yield return new WaitForSeconds(1.0f);
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

            terrainFaces[i] = new TerrainFace(meshFilters[i].sharedMesh, directions[i], size, this, devMode);
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
