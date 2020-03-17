using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] int worldSizeInChunks = 10;
    [SerializeField] float frequency = 85;

    Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();
    List<Vector3Int> chunkPositions = new List<Vector3Int>();
    bool hasGenerated = false;

    // Start is called before the first frame update
    void Start()
    {
        Generate();
    }

    private void Update()
    {
        if(hasGenerated == false)
        {
            GenerateChunkHeights();
        }
    }

    void Generate()
    {
        //Position setting for chunks
        for (int x = 0; x < worldSizeInChunks; x++)
        {
            for (int z = 0; z < worldSizeInChunks; z++)
            {
                //Array creation
                Vector3Int chunkPos = new Vector3Int(x * MarchingData.ChunkWidth, 0, z * MarchingData.ChunkWidth);
                chunkPositions.Add(chunkPos);
                chunks.Add(chunkPos, new Chunk(chunkPos, frequency));
                chunks[chunkPos].chunkObject.transform.SetParent(transform);
            }
        }

        //GenerateChunkHeights();
    }

    void GenerateChunkHeights()
    {
        //Create list for height gen jobs
        NativeList<JobHandle> heightGenJobs = new NativeList<JobHandle>(Allocator.Persistent);
        NativeArray<float> allHeights;
        NativeList<float2> chunkOffsets = new NativeList<float2>(Allocator.Persistent);

        for (int i = 0; i < chunkPositions.Count; i++)
        {
            chunkOffsets.Add(new float2(chunkPositions[i].x, chunkPositions[i].z));
        }

        List<JobHandle> tempHandles = chunks[new Vector3Int(0, 0, 0)].ScheduleChunkHeightDataGeneration(MarchingData.ChunkWidth, MarchingData.ChunkHeight, chunkOffsets, frequency, MarchingData.BaseTerrainHeight,
            MarchingData.TerrainHeightRange, out allHeights, chunks.Count, 1);

        for (int i = 0; i < tempHandles.Count; i++)
        {
            heightGenJobs.Add(tempHandles[i]);
        }

        JobHandle.CompleteAll(heightGenJobs);
        heightGenJobs.Dispose();

        allHeights.Dispose();
        chunkOffsets.Dispose();


        hasGenerated = true;
    }
}
