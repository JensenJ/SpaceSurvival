using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Entities;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Collections;
using Unity.Mathematics;

//Custom inspector button for generating within edit mode
[CustomEditor(typeof(GalaxyGenerator))]
public class GalaxyGenInspector : Editor
{
    public override void OnInspectorGUI()
    {
        //Draw default Test variables
        DrawDefaultInspector();

        //Button init
        GalaxyGenerator test = (GalaxyGenerator)target;
        if (GUILayout.Button("Generate"))
        {
            test.Generate();
        }
        if (GUILayout.Button("Clear Galaxy"))
        {
            test.ClearMap();
        }
    }
}

//Actual test class
public class GalaxyGenerator : MonoBehaviour
{
    //Galaxy spawn settings
    [Header("Galaxy Spawn Settings: ")]
    public float starSpawnRadius = 5;
    public Vector2 galaxySize = Vector2.one;
    public int galaxyRejectionSamples = 30;
    public float starDisplayRadius = 1;

    [Header("Celestial Entity Spawn Settings: ")]
    public Mesh starMesh;
    public Material starMaterial;

    [Header("Debug Settings: ")]
    public bool showDebugGizmos = false;
    //Lists for keeping track of positions
    List<Vector2> starPositions; //Single generation, keeps track of the positions of stars

    EntityManager entityManager;
    EntityArchetype starArchetype;

    List<Entity> starEntities;

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

    //Function to generate a Galaxy
    public void Generate()
    {
        //Checking values
        if (starSpawnRadius == 0)
        {
            return;
        }

        //Generate star positions
        starPositions = PoissonDiscSampler.GenerateSingleSample(starSpawnRadius, new Vector2(), galaxySize, 30);

        //If in play mode
        if (Application.isPlaying)
        {
            //Entity initialisation
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            starArchetype = entityManager.CreateArchetype(
                typeof(Translation),
                typeof(RenderMesh),
                typeof(LocalToWorld),
                typeof(RenderBounds),
                typeof(Scale),
                typeof(StarData)
            );

            starEntities = new List<Entity>();

            //Stars within each galaxy offset to fit within galaxy boundaries.
            //For each galaxy
            for (int i = 0; i < starPositions.Count; i++)
            {
                //Get star
                Vector2 star = starPositions[i];
                //Reassign star into array
                starPositions[i] = star;

                if (Application.isPlaying)
                {
                    Entity starEntity = GenerateStarEntity(i);

                    List<Entity> celestials = GenerateCelestials(starEntity); //TODO: Do something with celestials array, e.g. store for later deletion etc.

                    //Add sublist to entire list.
                    starEntities.Add(starEntity);
                }
            }
        }
    }

    //Function to generate star entity data
    private Entity GenerateStarEntity(int i)
    {
        //Entity creation and placement
        Entity starEntity = entityManager.CreateEntity(starArchetype);

        //Set position
        entityManager.SetComponentData(starEntity, new Translation
        {
            Value = new Vector3(starPositions[i].x, 0, starPositions[i].y)
        });

        //Set render settings
        entityManager.SetSharedComponentData(starEntity, new RenderMesh
        {
            mesh = starMesh,
            material = starMaterial,
            castShadows = UnityEngine.Rendering.ShadowCastingMode.On
        });

        //Generate star scale
        float starScale = UnityEngine.Random.Range(0.1f, 1.0f);

        //Set scale
        entityManager.SetComponentData(starEntity, new Scale
        {
            Value = starScale
        });

        //Generate star data
        StarData data = GalaxyData.CreateStarData(starScale);

        //Set stardata field
        entityManager.SetComponentData(starEntity, new StarData
        {
            starSize = data.starSize,
            starType = data.starType,
            starTemperature = data.starTemperature
        });

        return starEntity;
    }

    //Function that will be run on all stars to generate their orbiting celestial objects such as planets and asteroids
    private List<Entity> GenerateCelestials(Entity star)
    {
        Translation transform = entityManager.GetComponentData<Translation>(star);
        float3 position = transform.Value;

        StarData starData = entityManager.GetComponentData<StarData>(star);

        int terrestrialPlanetCount = UnityEngine.Random.Range(0, 5);
        int gasGiantPlanetCount = UnityEngine.Random.Range(0, 3);
        int iceGiantPlanetCount = UnityEngine.Random.Range(0, 2);

        //Celestial generation rules:
        //  As you move away from the star:
        //      Surface temperature decreases
        //      Planet size tends to increase
        //      Time to orbit increases as orbital speed decreases
        //      Terrestrial planets -> Gas Giants -> Ice Giants
        //      Between significant layers (for example between terrestrial planets and gas giants) there can be asteroid belts.
        //      Planets tend to have more moons
        //  Other Celestial Rules:
        //      Only gas giants can have rings
        //      Planets rotate anticlockwise
        //      Asteroid belts are very wide, but not very tall 

        //TODO: Recalculate distance correctly
        for (int i = 0; i < terrestrialPlanetCount; i++)
        {
            float planetDistance = 10 * (i + 1);
            float3 planetPosition = new float3(position.x + planetDistance, position.y, position.z);
            float distanceFromStar = math.distancesq(position * position, planetPosition * planetPosition);

            PlanetData planetData = new PlanetData
            {
                isRinged = false,
                planetOrbitDistance = distanceFromStar,
                planetSurfaceTemperature = starData.starTemperature,
            };

            //Debug.Log("PlanetData (Distance " + i + ")" + planetData.planetOrbitDistance);
            //Debug.Log("PlanetData (Temperature " + i + ")" + planetData.planetSurfaceTemperature);
        }

        return null;
    }

    //Function to remove all entities in the star entities array
    public void ClearMap()
    {
        //For every star
        for (int i = 0; i < starEntities.Count; i++)
        {
            entityManager.DestroyEntity(starEntities[i]);
        }

        //Reinit list
        starEntities = new List<Entity>();
    }
    //Draw debug symbols for now until entity system created that handles Galaxy generation
    private void OnDrawGizmos()
    {
        if (showDebugGizmos)
        {
            //Whole Galaxy boundary box
            Gizmos.DrawWireCube(galaxySize / 2, galaxySize);
            if (starPositions != null)
            {

                foreach (Vector2 star in starPositions)
                {
                    //Draw the star
                    Gizmos.DrawWireSphere(star, starDisplayRadius);
                }
            }
        }
    }
}