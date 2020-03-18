using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [SerializeField] int worldSizeInChunks = 10;
    [SerializeField] float frequency = 85;
    [SerializeField] bool smoothTerrain = true;

    Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

    // Start is called before the first frame update
    void Start()
    {
        Generate();
    }

    void Generate()
    {
        for (int x = 0; x < worldSizeInChunks; x++)
        {
            for (int z = 0; z < worldSizeInChunks; z++)
            {
                Vector3Int chunkPos = new Vector3Int(x * MarchingData.ChunkWidth, 0, z * MarchingData.ChunkWidth);
                chunks.Add(chunkPos, new Chunk(chunkPos, frequency, smoothTerrain));
                chunks[chunkPos].chunkObject.transform.SetParent(transform);
            }
        }
    }
}
