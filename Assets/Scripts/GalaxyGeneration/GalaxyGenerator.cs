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
    public float galaxyInnerRadius = 30;
    public float galaxyOuterRadius = 100;
    public bool generateEveryFrame = false;

    [Header("Celestial Entity Spawn Settings: ")]
    public Mesh starMesh;
    public Material starMaterial;

    public Mesh planetMesh;
    public Material planetMaterial;

    public Mesh asteroidMesh;
    public Material asteroidMaterial;

    [Header("Debug Settings: ")]
    public bool showDebugGizmos = false;
    //Lists for keeping track of positions
    List<Vector2> starPositions; //Single generation, keeps track of the positions of stars

    EntityManager entityManager;
    EntityArchetype starArchetype;
    EntityArchetype planetArchetype;
    EntityArchetype asteroidArchetype;

    List<Entity> starEntities;
    List<List<Entity>> celestialEntities;

    //When editor refreshes
    private void OnValidate()
    {
        Generate();
    }

    private void Start()
    {
        Generate();
    }

    //Every frame
    private void Update() {
        if (generateEveryFrame)
        {
            Generate();
        }
    }

    //Function to generate a Galaxy
    public void Generate()
    {
        //Clear map before generating
        ClearMap();

        //Checking values
        if (starSpawnRadius == 0)
        {
            return;
        }

        //Generate star positions
        starPositions = PoissonDiscSampler.GenerateSingleSample(starSpawnRadius, new Vector2(), galaxySize, true, galaxyInnerRadius, galaxyOuterRadius, galaxyRejectionSamples);

        Debug.Log("Generation Count: " + starPositions.Count);

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
                typeof(Rotation),
                typeof(RenderMesh),
                typeof(LocalToWorld),
                typeof(RenderBounds),
                typeof(Scale),
                typeof(PlanetData)
            );

            asteroidArchetype = entityManager.CreateArchetype(
                typeof(Translation),
                typeof(Rotation),
                typeof(RenderMesh),
                typeof(LocalToWorld),
                typeof(RenderBounds),
                typeof(Scale)
            );

            starEntities = new List<Entity>();
            celestialEntities = new List<List<Entity>>();

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
        entityManager.SetName(starEntity, "Star");

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
        });

        List<Entity> celestials = GenerateCelestials(starEntity); //TODO: Do something with array, store for later destruction

        celestialEntities.Add(celestials);

        
        return starEntity;
    }

    //Function that will be run on all stars to generate their orbiting celestial objects such as planets and asteroids
    private List<Entity> GenerateCelestials(Entity star)
    {
        Translation transform = entityManager.GetComponentData<Translation>(star);
        float3 starPosition = transform.Value;

        StarData starData = entityManager.GetComponentData<StarData>(star);

        int terrestrialPlanetCount = UnityEngine.Random.Range(0, 5);
        int asteroidBeltGeneration = UnityEngine.Random.Range(0, 100);

        bool hasAsteroidBelt = false;

        if(asteroidBeltGeneration >= 50)
        {
            hasAsteroidBelt = true;
        }

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
        //      Only gas/ice giants can have rings
        //      Planets rotate anticlockwise
        //      Asteroid belts are very wide, but not very tall 

        //Debug.Log("StarType = " + starData.starType + ", Size = " + starData.starSize);

        //Celestial Entity List Generation
        List<Entity> celestialList = new List<Entity>();

        //Terrestrial Planets
        List<Entity> terrestrialPlanets = GenerateTerrestrialPlanets(terrestrialPlanetCount, starPosition, starData.starSize);
        for (int i = 0; i < terrestrialPlanets.Count; i++)
        {
            celestialList.Add(terrestrialPlanets[i]);
        }

        //Asteroid Belt between terrestrial and ice giants
        List<Entity> asteroidBelt = new List<Entity>();
        if (hasAsteroidBelt)
        {
            asteroidBelt = GenerateAsteroidBelt(starPosition);
            for (int i = 0; i < asteroidBelt.Count; i++)
            {
                celestialList.Add(asteroidBelt[i]);
            }
        }

        return celestialList;
    }

    //Function to generate the terrestrial planets within a solar system
    public List<Entity> GenerateTerrestrialPlanets(int terrestrialPlanetCount, float3 starPosition, float starSize)
    {
        List<Entity> terrestrialPlanets = new List<Entity>();

        float terrestrialBaseDistanceMultiplier = 1.57f;
        float terrestrialDistanceUncertainty = 0.2f;

        float distance = UnityEngine.Random.Range(3.5f, 3.65f) - (0.75f * terrestrialBaseDistanceMultiplier);

        for (int i = 0; i < terrestrialPlanetCount; i++)
        {
            //Distance Calculation
            distance *= (terrestrialBaseDistanceMultiplier + UnityEngine.Random.Range(-terrestrialDistanceUncertainty / 2, terrestrialDistanceUncertainty));
            float3 planetPosition = new float3(starPosition.x + distance, starPosition.y, starPosition.z);
            float distanceFromStar = math.distance(starPosition, planetPosition);

            //Temperature variable generation
            float greenhouseEffect = UnityEngine.Random.Range(1.0f, 30.0f); //Higher this is, the higher the temperature
            float albedo = UnityEngine.Random.Range(0.0f, 40.0f); //Lower this is, the higher the temperature

            //Get temperature
            float temperature = GetPlanetSurfaceTemperature(starSize, distanceFromStar, albedo, greenhouseEffect);
            if (temperature < 0)
            {
                temperature = 0;
            }

            Entity planetEntity = GeneratePlanetEntity(planetPosition, distanceFromStar, temperature, albedo, greenhouseEffect);

            terrestrialPlanets.Add(planetEntity);
        }
        return terrestrialPlanets;
    }

    public List<Entity> GenerateAsteroidBelt(float3 starPosition)
    {
        List<Entity> asteroidBelt = new List<Entity>();

        //Generate asteroid positions
        //TODO: Generate proper values randomly for asteroid belt, such as width etc.

        Vector2 beltSize = new Vector2(50, 50);

        List<Vector2> asteroidPositions = PoissonDiscSampler.GenerateSingleSample(2.5f, new Vector2(starPosition.x, starPosition.z), beltSize, true, 20, 24, 30);

        

        for (int i = 0; i < asteroidPositions.Count; i++)
        {

            float3 asteroidPosition = new float3(asteroidPositions[i].x, 0, asteroidPositions[i].y) - (new float3(beltSize.x, 0, beltSize.y) / 2);


            //Entity creation and placement
            Entity asteroidEntity = entityManager.CreateEntity(asteroidArchetype);
            entityManager.SetName(asteroidEntity, "Asteroid");

            //Set position
            entityManager.SetComponentData(asteroidEntity, new Translation
            {
                Value = asteroidPosition,
            });

            //Set render settings
            entityManager.SetSharedComponentData(asteroidEntity, new RenderMesh
            {
                mesh = asteroidMesh,
                material = asteroidMaterial,
                castShadows = UnityEngine.Rendering.ShadowCastingMode.On
            });

            float asteroidSize = UnityEngine.Random.Range(0.1f, 1.0f);

            //Set scale
            entityManager.SetComponentData(asteroidEntity, new Scale
            {
                Value = asteroidSize
            });
            asteroidBelt.Add(asteroidEntity);
        }


        return asteroidBelt;
    }

    //Function to generate a planet entity when given all relevant data
    public Entity GeneratePlanetEntity(float3 planetPosition, float distanceFromStar, float temperature, float albedo, float greenhouseEffect)
    {
        //Entity creation and placement
        Entity planetEntity = entityManager.CreateEntity(planetArchetype);
        entityManager.SetName(planetEntity, "Planet");

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
            planetOrbitTime = distanceFromStar * 40,
        });
        return planetEntity;
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
        if (Application.isPlaying)
        {
            if (starEntities != null)
            {

                //For every star
                for (int i = 0; i < starEntities.Count; i++)
                {
                    //Destroy star entity
                    entityManager.DestroyEntity(starEntities[i]);
                }
            }

            if (celestialEntities != null)
            {
                //For every star/planet
                for (int i = 0; i < celestialEntities.Count; i++)
                {
                    //For every planet
                    for (int j = 0; j < celestialEntities[i].Count; j++)
                    {
                        //Destroy planet entity
                        entityManager.DestroyEntity(celestialEntities[i][j]);
                    }
                }
            }

            
        }

        //Reinit lists
        starEntities = new List<Entity>();
        celestialEntities = new List<List<Entity>>();
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