using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Entities;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Collections;

//Custom inspector button for generating within edit mode
[CustomEditor(typeof(UniverseGenerator))]
public class UniverseGenInspector : Editor
{
    
    public override void OnInspectorGUI()
    {
        //Draw default Test variables
        DrawDefaultInspector();

        //Button init
        UniverseGenerator test = (UniverseGenerator)target;
        if (GUILayout.Button("Generate"))
        {
            test.Generate();
        }
        if (GUILayout.Button("Clear Universe"))
        {
            test.ClearMap();
        }
    }
}

//Actual test class
public class UniverseGenerator : MonoBehaviour
{
    //Galaxy spawn settings
    [Header("Galaxy Spawn Settings: ")]
    public float galaxyRadius = 5;
    public Vector2 galaxySize = Vector2.one;
    public int galaxyRejectionSamples = 30;
    public float galaxyDisplayRadius = 1;

    //Solar system settings
    [Header("Solar System Spawn Settings: ")]
    public float solarRadius = 1;
    public Vector2 solarSize = Vector2.one;
    public int solarRejectionSamples = 30;
    public float solarDisplayRadius = 0.5f;

    [Header("Celestial Entity Spawn Settings: ")]
    public Mesh starMesh;
    public Material starMaterial;

    [Header("Debug Settings: ")]
    public bool showDebugGizmos = false;
    //Lists for keeping track of positions
    List<List<Vector2>> galaxies; //Multi generation, keeps track of the positions of stars within each galaxy
    List<Vector2> galaxyOrigins; //Single generation, keeps track of the positions of galaxies, used to calculate star / solar system positions

    EntityManager entityManager;
    EntityArchetype starArchetype;

    List<List<Entity>> starEntities;

    //When editor refreshes
    private void OnValidate()
    {
        Generate();
    }

    //Every frame
    private void Update()
    {
        ClearMap();
        Generate();
    }

    //Function to generate a universe
    public void Generate()
    {
        //Checking values
        if (galaxyRadius == 0 || solarRadius == 0)
        {
            return;
        }

        //Galaxy origin positions
        galaxyOrigins = PoissonDiscSampler.GenerateSingleSample(galaxyRadius, new Vector2(0, 0), galaxySize, galaxyRejectionSamples);

        //Galaxy offset to keep it within boundaries
        //For every galaxy
        for (int i = 0; i < galaxyOrigins.Count; i++)
        {
            //Get galaxy origin position
            Vector2 galaxy = galaxyOrigins[i];
            //Apply galaxy offset
            galaxy += (solarSize / 2);
            galaxyOrigins[i] = galaxy;
        }

        //Setting up galaxy and data, this can be used in future to add randomness such as different radii, galaxy sizes etc.
        float[] radii = new float[galaxyOrigins.Count];
        Vector2[] sampleSizes = new Vector2[galaxyOrigins.Count];
        //Data filling
        for (int i = 0; i < radii.Length; i++)
        {
            radii[i] = solarRadius;
            sampleSizes[i] = solarSize;
        }

        //Generate multiple galaxy samples from an array
        galaxies = PoissonDiscSampler.GenerateMultiSample(radii, galaxyOrigins.ToArray(), sampleSizes, solarRejectionSamples);

        if (Application.isPlaying)
        {
            //Entity initialisation
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            starArchetype = entityManager.CreateArchetype(
                typeof(Translation),
                typeof(RenderMesh),
                typeof(LocalToWorld),
                typeof(RenderBounds)
            );
        }

        starEntities = new List<List<Entity>>();

        //Stars within each galaxy offset to fit within galaxy boundaries.
        //For each galaxy
        for (int i = 0; i < galaxies.Count; i++)
        {
            //For each star
            List<Entity> starEntitiesForGalaxy = new List<Entity>();
            for (int j = 0; j < galaxies[i].Count; j++)
            {
                //Get star
                Vector2 star = galaxies[i][j];
                //Apply offset to star
                star += (solarSize / 2) - solarSize;
                //Reassign star into array
                galaxies[i][j] = star;

                if (Application.isPlaying)
                {
                    //Entity creation and placement
                    Entity starEntity = entityManager.CreateEntity(starArchetype);

                    //Set position
                    entityManager.SetComponentData(starEntity, new Translation
                    {
                        Value = new Vector3(galaxies[i][j].x, galaxies[i][j].y, 0)
                    }); ;

                    entityManager.SetSharedComponentData(starEntity, new RenderMesh
                    {
                        mesh = starMesh,
                        material = starMaterial,
                        castShadows = UnityEngine.Rendering.ShadowCastingMode.On
                    });

                    starEntitiesForGalaxy.Add(starEntity);
                    
                }
            }

            starEntities.Add(starEntitiesForGalaxy);
        }
    }

    public void ClearMap()
    {
        for (int i = 0; i < starEntities.Count; i++)
        {
            for (int j= 0; j < starEntities[i].Count; j++)
            {
                Entity star = starEntities[i][j];
                entityManager.DestroyEntity(star);
            }
        }

        starEntities = new List<List<Entity>>();
    }
    //Draw debug symbols for now until entity system created that handles universe generation
    private void OnDrawGizmos()
    {
        if (showDebugGizmos)
        {
            //Whole universe boundary box
            Gizmos.DrawWireCube((galaxySize + solarSize) / 2, galaxySize + solarSize);
            if (galaxies != null)
            {
                //For every galaxy
                foreach (List<Vector2> galaxy in galaxies)
                {
                    //For every star / solar system
                    foreach (Vector2 star in galaxy)
                    {
                        //Draw the star
                        Gizmos.DrawWireSphere(star, solarDisplayRadius);
                    }
                }

                //For every galaxy
                foreach (Vector2 galaxy in galaxyOrigins)
                {
                    //Draw bounding box and galaxy origin
                    Gizmos.DrawWireSphere(galaxy, galaxyDisplayRadius);
                    Gizmos.DrawWireCube(galaxy, solarSize);
                }
            }
        }
    }
}