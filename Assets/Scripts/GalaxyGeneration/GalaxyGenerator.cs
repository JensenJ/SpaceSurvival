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

    public Mesh terrestrialMesh;
    public Material terrestrialMaterial;

    public Mesh gasMesh;
    public Material gasMaterial;

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
        //Generate();
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
        starPositions = PoissonDiscSampler.GenerateRingedSingleSample(starSpawnRadius, new Vector2(), galaxySize, true, galaxyInnerRadius, galaxyOuterRadius, galaxyRejectionSamples);
        Debug.Log("Generation Count: " + starPositions.Count);

        //If in play mode
        if (Application.isPlaying)
        {
            //Entity manager initialisation
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            //Star archetype
            starArchetype = entityManager.CreateArchetype(
                typeof(Translation),
                typeof(RenderMesh),
                typeof(LocalToWorld),
                typeof(RenderBounds),
                typeof(Scale),
                typeof(StarData)
            );
            //Planet archetype
            planetArchetype = entityManager.CreateArchetype(
                typeof(Translation),
                typeof(Rotation),
                typeof(RenderMesh),
                typeof(LocalToWorld),
                typeof(RenderBounds),
                typeof(Scale),
                typeof(PlanetData)
            );
            //Asteroid archetype
            asteroidArchetype = entityManager.CreateArchetype(
                typeof(Translation),
                typeof(Rotation),
                typeof(RenderMesh),
                typeof(LocalToWorld),
                typeof(RenderBounds),
                typeof(Scale)
            );

            //List creation
            starEntities = new List<Entity>();
            celestialEntities = new List<List<Entity>>();

            //Generate galaxy entities
            if (Application.isPlaying)
            {
                starEntities = GenerateStarEntities(starPositions);
            }
        }
    }

    //Function to generate star entity data
    private List<Entity> GenerateStarEntities(List<Vector2> starPositions)
    {
        //Create list of star entities
        List<Entity> starEntities = new List<Entity>();

        //For every star position
        for (int i = 0; i < starPositions.Count; i++)
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

            //Add star entity to list
            starEntities.Add(starEntity);
        }

        //Create celestials and add to global array for later destruction / management
        celestialEntities.Add(GenerateCelestials(starEntities));
        
        //Return entity list
        return starEntities;
    }

    //Function that will be run on all stars to generate their orbiting celestial objects such as planets and asteroids
    private List<Entity> GenerateCelestials(List<Entity> stars)
    {
        //List initialisation
        List<float3> starPositions = new List<float3>();
        List<float> starSizes = new List<float>();
        List<int> terrestrialPlanetCounts = new List<int>();
        List<bool> asteroidBeltStatus = new List<bool>();
        List<int> gasGiantPlanetCounts = new List<int>();
        List<int> iceGiantPlanetCounts = new List<int>();

        for (int i = 0; i < stars.Count; i++)
        {
            //Get position and add to array
            Translation transform = entityManager.GetComponentData<Translation>(stars[i]);
            starPositions.Add(transform.Value);

            StarData starData = entityManager.GetComponentData<StarData>(stars[i]);
            starSizes.Add(starData.starSize);

            //Generates terrestial planet count
            terrestrialPlanetCounts.Add(UnityEngine.Random.Range(0, 5));
            //terrestrialPlanetCounts.Add(4);

            //Asteroid belt generation chooser
            int asteroidBeltGeneration = UnityEngine.Random.Range(0, 100);
            bool hasAsteroidBelt = false;
            if(asteroidBeltGeneration >= 50)
            {
                hasAsteroidBelt = true;
            }
            asteroidBeltStatus.Add(hasAsteroidBelt);

            gasGiantPlanetCounts.Add(UnityEngine.Random.Range(0, 4));
            //gasGiantPlanetCounts.Add(3);
            iceGiantPlanetCounts.Add(UnityEngine.Random.Range(0, 3));
            //iceGiantPlanetCounts.Add(3);
        }

        //Celestial generation rules:
        //  As you move away from the star:
        //      Surface temperature decreases (not always on atmospheric planets as they have greenhouse effect / albedo)
        //      Planet size tends to increase
        //      Time to orbit increases as orbital speed decreases
        //      Terrestrial planets -> Gas Giants -> Ice Giants
        //      Planets tend to have more moons
        //  Other Celestial Rules:
        //      Only gas/ice giants can have rings
        //      Between significant layers (for example between terrestrial planets and gas giants) there can be asteroid belts.
        //      Planets rotate anticlockwise
        //      Asteroid belts are very wide, but not very tall 

        //Celestial Entity List Generation
        List<Entity> celestialList = new List<Entity>();

        //Terrestrial Planets
        for (int i = 0; i < terrestrialPlanetCounts.Count; i++)
        {
            List<Entity> terrestrialPlanets = GenerateTerrestrialPlanets(terrestrialPlanetCounts[i], starPositions[i], starSizes[i]);
            for (int j = 0; j < terrestrialPlanets.Count; j++)
            {
                celestialList.Add(terrestrialPlanets[j]);
            }
        }

        //Asteroid Belt between terrestrial and ice giants
        List<float3> beltStarPositions = new List<float3>();

        for (int i = 0; i < asteroidBeltStatus.Count; i++)
        {
            //If has an asteroid belt
            if (asteroidBeltStatus[i] == true)
            {
                //Add to belt star positions
                beltStarPositions.Add(starPositions[i]);
            }
        }

        //Gas Giant Planets
        for (int i = 0; i < gasGiantPlanetCounts.Count; i++)
        {
            List<Entity> gasPlanets = GenerateGasPlanets(gasGiantPlanetCounts[i], starPositions[i], starSizes[i], terrestrialPlanetCounts[i]);
            for (int j = 0; j < gasPlanets.Count; j++)
            {
                celestialList.Add(gasPlanets[j]);
            }
        }

        //Generate asteroid belt entities
        List<List<Entity>> asteroidBelts = GenerateAsteroidBelt(beltStarPositions, terrestrialPlanetCounts);


        //Add asteroid belt entities to the celestial list
        for (int i = 0; i < asteroidBelts.Count; i++)
        {
            for (int j = 0; j < asteroidBelts[i].Count; j++)
            {
                celestialList.Add(asteroidBelts[i][j]);
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

            Entity planetEntity = GeneratePlanetEntity(planetPosition, distanceFromStar, starSize, albedo, greenhouseEffect, 0.1f, 0.3f, terrestrialMesh, terrestrialMaterial);

            terrestrialPlanets.Add(planetEntity);
        }
        return terrestrialPlanets;
    }

    //Function to generate the gas planets within a solar system.
    public List<Entity> GenerateGasPlanets(int gasPlanetCount, float3 starPosition, float starSize, float terrestrialCount)
    {
        List<Entity> gasPlanets = new List<Entity>();

        float gasBaseDistanceMultiplier = 1.83f;
        float gasDistanceUncertainty = 0.1f;

        float distance = UnityEngine.Random.Range(9.5203f, 9.65f) - (0.75f * gasBaseDistanceMultiplier) + (terrestrialCount * 1.77f);

        for (int i = 0; i < gasPlanetCount; i++)
        {
            //Distance Calculation
            distance *= (gasBaseDistanceMultiplier + UnityEngine.Random.Range(-gasDistanceUncertainty / 2, gasDistanceUncertainty));
            float3 planetPosition = new float3(starPosition.x + distance, starPosition.y, starPosition.z);
            float distanceFromStar = math.distance(starPosition, planetPosition);

            //Temperature variable generation
            float greenhouseEffect = UnityEngine.Random.Range(1.0f, 30.0f); //Higher this is, the higher the temperature
            float albedo = UnityEngine.Random.Range(0.0f, 40.0f); //Lower this is, the higher the temperature

            Entity planetEntity = GeneratePlanetEntity(planetPosition, distanceFromStar, starSize, albedo, greenhouseEffect, 0.5f, 0.9f, gasMesh, gasMaterial);

            gasPlanets.Add(planetEntity);
        }
        return gasPlanets;
    }

    public List<List<Entity>> GenerateAsteroidBelt(List<float3> starPositions, List<int> terrestrialPlanetCount)
    {
        List<List<Entity>> asteroidBelts = new List<List<Entity>>();

        //Generate asteroid data for poisson sampler
        Vector2[] positions = new Vector2[starPositions.Count];
        Vector2[] beltSizes = new Vector2[starPositions.Count];

        float[] ringInnerRadii = new float[starPositions.Count];
        float[] ringOuterRadii = new float[starPositions.Count];

        float[] radii = new float[starPositions.Count];

        for (int i = 0; i < starPositions.Count; i++)
        {
            //Fill arrays
            positions[i] = new Vector2(starPositions[i].x, starPositions[i].z);
            float beltSize = UnityEngine.Random.Range((terrestrialPlanetCount[i] * 10f) + 2f, (terrestrialPlanetCount[i] * 10f) + (terrestrialPlanetCount[i])) + 5f;
            beltSizes[i] = new Vector2(beltSize, beltSize);
            //ringInnerRadii[i] = beltSize / 2 - (beltSize * 0.15f) + 5.5f;
            //ringOuterRadii[i] = beltSize / 2 - (beltSize * 0.1f) + 6.5f;

            //TODO: Tweak values to make planets not collide with asteroid field
            ringInnerRadii[i] = terrestrialPlanetCount[i] * 0.17f * (beltSize / 2) + 2.1f;
            ringOuterRadii[i] = terrestrialPlanetCount[i] * 0.21f * (beltSize / 2) + 4.3f; 
            radii[i] = UnityEngine.Random.Range(0.5f, 2.5f);
        }

        //Poisson sampler
        List<List<Vector2>> asteroidPositions = PoissonDiscSampler.GenerateRingedMultiSample(radii, positions, beltSizes, true, ringInnerRadii, ringOuterRadii, 30);
        
        //For every star
        for (int i = 0; i < asteroidPositions.Count; i++)
        {
            //Sub list creation
            List<Entity> asteroidBeltForStar = new List<Entity>();
            //For every asteroid in orbit of that star
            for (int j = 0; j < asteroidPositions[i].Count; j++)
            {
                //Get asteroid position
                float3 asteroidPosition = new float3(asteroidPositions[i][j].x, 0, asteroidPositions[i][j].y) - (new float3(beltSizes[i].x, 0, beltSizes[i].y) / 2);

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

                float asteroidSize = UnityEngine.Random.Range(0.1f, 0.2f);

                //Set scale
                entityManager.SetComponentData(asteroidEntity, new Scale
                {
                    Value = asteroidSize
                });

                //Add to sublist
                asteroidBeltForStar.Add(asteroidEntity);
            }
            //Add to main list
            asteroidBelts.Add(asteroidBeltForStar);
        }

        return asteroidBelts;
    }

    //Function to generate a planet entity when given all relevant data
    public Entity GeneratePlanetEntity(float3 planetPosition, float distanceFromStar, float starSize, float albedo, float greenhouseEffect, float minPlanetSize, float maxPlanetSize, Mesh planetMesh, Material planetMaterial)
    {
        //Entity creation and placement
        Entity planetEntity = entityManager.CreateEntity(planetArchetype);
        entityManager.SetName(planetEntity, "Planet");

        float temperature = GetPlanetSurfaceTemperature(starSize, distanceFromStar, albedo, greenhouseEffect);
        if (temperature < 0)
        {
            temperature = 0;
        }

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

        float planetSize = UnityEngine.Random.Range(minPlanetSize, maxPlanetSize);

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
        //Return the planet entity
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