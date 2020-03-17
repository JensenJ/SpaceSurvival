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

    // Start is called before the first frame update
    void Start()
    {
        Generate();
    }

    private void Update()
    {
        GenerateChunkHeights();
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
                chunks.Add(chunkPos, new Chunk(chunkPos, frequency));
                chunks[chunkPos].chunkObject.transform.SetParent(transform);
            }
        }

        GenerateChunkHeights();
    }

    void GenerateChunkHeights()
    {
        //Create list for height gen jobs
        NativeList<JobHandle> heightGenJobs = new NativeList<JobHandle>(Allocator.Temp);
        List<NativeList<float>> allHeights = new List<NativeList<float>>();

        List<JobHandle> tempHandles = chunks[new Vector3Int(0, 0, 0)].ScheduleChunkHeightDataGeneration(MarchingData.ChunkWidth, MarchingData.ChunkHeight, frequency, MarchingData.BaseTerrainHeight,
            MarchingData.TerrainHeightRange, out allHeights);

        for (int i = 0; i < tempHandles.Count; i++)
        {
            heightGenJobs.Add(tempHandles[i]);
        }

        JobHandle.CompleteAll(heightGenJobs);
        heightGenJobs.Dispose();

        for (int i = 0; i < allHeights.Count; i++)
        {
            allHeights[i].Dispose();
        }
    }
}
