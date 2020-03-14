﻿using System.Collections;
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

    public Mesh planetMesh;
    public Material planetMaterial;

    [Header("Debug Settings: ")]
    public bool showDebugGizmos = false;
    //Lists for keeping track of positions
    List<Vector2> starPositions; //Single generation, keeps track of the positions of stars

    EntityManager entityManager;
    EntityArchetype starArchetype;
    EntityArchetype planetArchetype;

    List<Entity> starEntities;
    List<List<Entity>> planetEntities;

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

            planetArchetype = entityManager.CreateArchetype(
                typeof(Translation),
                typeof(RenderMesh),
                typeof(LocalToWorld),
                typeof(RenderBounds),
                typeof(Scale),
                typeof(PlanetData)
            );

            starEntities = new List<Entity>();
            planetEntities = new List<List<Entity>>();

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
        List<Entity> celestials = GenerateCelestials(starEntity); //TODO: Do something with array, store for later destruction

        planetEntities.Add(celestials);

        //Set stardata field
        entityManager.SetComponentData(starEntity, new StarData
        {
            starSize = data.starSize,
            starType = data.starType,
        });
        
        return starEntity;
    }

    //Function that will be run on all stars to generate their orbiting celestial objects such as planets and asteroids
    private List<Entity> GenerateCelestials(Entity star)
    {
        Translation transform = entityManager.GetComponentData<Translation>(star);
        float3 position = transform.Value;

        StarData starData = entityManager.GetComponentData<StarData>(star);

        //int terrestrialPlanetCount = UnityEngine.Random.Range(0, 5);
        int terrestrialPlanetCount = 4;
        int gasGiantPlanetCount = UnityEngine.Random.Range(0, 3);
        int iceGiantPlanetCount = UnityEngine.Random.Range(0, 2);

        //Celestial generation rules:
        //  As you move away from the star:
        //      Surface temperature decreases (not always on atmospheric planets as they have greenhouse effect / albedo)
        //      Planet size tends to increase
        //      Time to orbit increases as orbital speed decreases
        //      Terrestrial planets -> Gas Giants -> Ice Giants
        //      Between significant layers (for example between terrestrial planets and gas giants) there can be asteroid belts.
        //      Planets tend to have more moons
        //  Other Celestial Rules:
        //      Only gas giants can have rings
        //      Planets rotate anticlockwise
        //      Asteroid belts are very wide, but not very tall 

        float terrestrialBaseDistanceMultiplier = 1.57f;
        float terrestrialDistanceUncertainty = 0.2f;

        float distance = UnityEngine.Random.Range(3.5f, 3.65f) - (0.75f * terrestrialBaseDistanceMultiplier);

        //Debug.Log("StarType = " + starData.starType + ", Size = " + starData.starSize);

        List<Entity> planetList = new List<Entity>();

        for (int i = 0; i < terrestrialPlanetCount; i++)
        {
            distance *= (terrestrialBaseDistanceMultiplier + UnityEngine.Random.Range(-terrestrialDistanceUncertainty / 2, terrestrialDistanceUncertainty));
            float3 planetPosition = new float3(position.x + distance, position.y, position.z);
            float distanceFromStar = math.distance(position, planetPosition);

            //Temperature variable generation
            float greenhouseEffect = UnityEngine.Random.Range(1.0f, 30.0f); //Higher this is, the higher the temperature
            float albedo = UnityEngine.Random.Range(0.0f, 40.0f); //Lower this is, the higher the temperature

            //Get temperature
            float temperature = GetPlanetSurfaceTemperature(starData.starSize, distanceFromStar, albedo, greenhouseEffect);
            if (temperature < 0)
            {
                temperature = 0;
            }

            //Debug.Log("PlanetData (Distance " + i + ")" + planetData.planetOrbitDistance);
            //Debug.Log("PlanetData (Temperature " + i + ")" + (planetData.planetSurfaceTemperature -273) + "C");
            //Debug.Log("PlanetData (OrbitSpeed " + i + ")" + planetData.planetOrbitTime + " days");

            //Entity creation and placement
            Entity planetEntity = entityManager.CreateEntity(planetArchetype);

            //Set position
            entityManager.SetComponentData(planetEntity, new Translation
            {
                Value = planetPosition
            });

            //Set render settings
            entityManager.SetSharedComponentData(planetEntity, new RenderMesh
            {
                mesh = planetMesh,
                material = planetMaterial,
                castShadows = UnityEngine.Rendering.ShadowCastingMode.On
            });

            float planetSize = UnityEngine.Random.Range(0.1f, 1.0f);

            //Set scale
            entityManager.SetComponentData(planetEntity, new Scale
            {
                Value = planetSize
            });

            //Set other planet data
            entityManager.SetComponentData(planetEntity, new PlanetData
            {
                isRinged = false,
                planetOrbitDistance = distanceFromStar,
                planetSurfaceTemperature = temperature,
                surfaceAlbedo = albedo,
                greenhouseEffect = greenhouseEffect,
                planetSize = planetSize,
                planetType = PlanetType.Terrestrial,
                planetRotationSpeed = UnityEngine.Random.Range(1.0f, 250.0f),
                planetOrbitTime = distance * 40,
            });
            planetList.Add(planetEntity);
        }


        return planetList;
    }

    //Function to return the planet's surface temperature when given information about the planet
    public float GetPlanetSurfaceTemperature(float size, float distanceFromStar, float albedo, float greenhouse)
    {
        float pi = math.PI;
        float sigma = 5.6703f * math.pow(10, -5);
        float L = 3.846f * math.pow(10, 33) * math.pow(size, 3);
        float D = distanceFromStar * 1.496f * math.pow(10, 13);
        float A = albedo / 100;
        float T = greenhouse * 0.5841f;
        float X = math.sqrt((1 - A) * L / (16 * pi * sigma));
        float T_eff = math.sqrt(X) * (1 / math.sqrt(D));
        float T_eq = (math.pow(T_eff, 4)) * (1 + (3 * T / 4));
        float T_sur = T_eq / 0.9f;
        float T_kel = math.sqrt(math.sqrt(T_sur));
        T_kel = math.round(T_kel);
        return T_kel + 250;
    }

    //Function to remove all entities in the star entities array
    public void ClearMap()
    {
        //For every star
        for (int i = 0; i < starEntities.Count; i++)
        {
            //Destroy star entity
            entityManager.DestroyEntity(starEntities[i]);
        }
        //For every star/planet
        for (int i = 0; i < planetEntities.Count; i++)
        {
            //For every planet
            for (int j = 0; j < planetEntities[i].Count; j++)
            {
                //Destroy planet entity
                entityManager.DestroyEntity(planetEntities[i][j]);
            }
        }

        //Reinit lists
        starEntities = new List<Entity>();
        planetEntities = new List<List<Entity>>();
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