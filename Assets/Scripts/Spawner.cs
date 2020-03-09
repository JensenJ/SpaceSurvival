using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Rendering;

public class Spawner : MonoBehaviour
{
    [SerializeField] private Mesh mesh = null;
    [SerializeField] private Material material = null;
    [SerializeField] private GameObject gameObjectPrefab = null;
    [SerializeField] private int dimX = 20;
    [SerializeField] private int dimY = 20;
    [SerializeField] private float spacing = 1.0f;

    private Entity entityPrefab;
    private World defaultWorld;
    private EntityManager entityManager;

    private void Start()
    {
        defaultWorld = World.DefaultGameObjectInjectionWorld;
        entityManager = defaultWorld.EntityManager;

        GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(defaultWorld, null);
        entityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(gameObjectPrefab, settings);

        InstantiateEntityGrid(dimX, dimY, spacing);
    }

    private void InstantiateEntity(float3 position)
    {
        Entity myEntity = entityManager.Instantiate(entityPrefab);
        entityManager.SetComponentData(myEntity, new Translation { Value = position });
    }

    private void InstantiateEntityGrid(int dimX, int dimY, float spacing = 1f)
    {
        for (int i = 0; i < dimX; i++)
        {
            for (int j = 0; j < dimY; j++)
            {
                //MakeEntity(new float3(i * spacing, j * spacing, 0f));
                InstantiateEntity(new float3(i * spacing, j * spacing, 0f));
            }
        }
    }

    private void MakeEntity(float3 position)
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityArchetype entityArchetype = entityManager.CreateArchetype(
            typeof(Translation),
            typeof(RenderMesh),
            typeof(LocalToWorld),
            typeof(RenderBounds),
            typeof(MoveSpeed),
            typeof(WaveData)
        );

        NativeArray<Entity> entityArray = new NativeArray<Entity>(1, Allocator.Temp);
        entityManager.CreateEntity(entityArchetype, entityArray);

        for (int i = 0; i < entityArray.Length; i++)
        {
            Entity entity = entityArray[i];

            //Set position
            entityManager.SetComponentData(entity, new Translation
            {
                Value = position
            });

            entityManager.SetSharedComponentData(entity, new RenderMesh
            {
                mesh = mesh,
                material = material,
                castShadows = UnityEngine.Rendering.ShadowCastingMode.On
            });

            entityManager.SetComponentData(entity, new MoveSpeed 
            { 
                Value = 3.0f
            });

            entityManager.SetComponentData(entity, new WaveData
            {
                amplitude = 2.0f,
                yOffset = 0.0f,
                xOffset = 0.0f
            });
        }

        entityArray.Dispose();

    }
}