using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;

//A class to generate a mesh from cube data
public class Chunk
{
    //Mesh data
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<Vector2> uvs = new List<Vector2>();

    public GameObject chunkObject;
    MeshFilter meshFilter;
    MeshCollider meshCollider;
    MeshRenderer meshRenderer;

    //Terrain variables
    float terrainSurface = 0.5f;

    int width { get { return MarchingData.ChunkWidth; } }
    int height { get { return MarchingData.ChunkHeight; } }
    float terrainHeight { get { return MarchingData.terrainSurface; } }

    float[,,] terrainMap;

    public float frequency;

    public Vector3Int chunkPosition;

    public Chunk(Vector3Int position, float _frequency)
    {
        chunkObject = new GameObject();
        chunkPosition = position;
        chunkObject.transform.position = chunkPosition;
        chunkObject.name = string.Format("Chunk {0}, {1}", position.x, position.z);

        frequency = _frequency;

        //Create arrays and get components
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshCollider = chunkObject.AddComponent<MeshCollider>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();
        meshRenderer.material = Resources.Load<Material>("Materials/Terrain");
        chunkObject.transform.tag = "Terrain";

        terrainMap = new float[width + 1, height + 1, width + 1];
        //CreateMeshData();

        ClearMeshData();
        
        BuildMesh();
    }

    void GenerateChunkData()
    {

        //NativeList<Vector3> jobVertices = new NativeList<Vector3>(Allocator.TempJob);
        //NativeList<int> jobTriangles = new NativeList<int>(Allocator.TempJob);

        //GenerateChunkDataJob chunkJob = new GenerateChunkDataJob()
        //{
        //    width = width,
        //    height = height,
        //    frequency = frequency,
        //    surfaceLevel = terrainSurface,
        //    chunkPosition = chunkPosition,
        //    vertices = jobVertices,
        //    triangles = jobTriangles,
        //};

        //JobHandle chunkHandle = chunkJob.Schedule();
        //chunkHandle.Complete();

        //for (int i = 0; i < jobVertices.Length; i++)
        //{
        //    vertices.Add(jobVertices[i]);
        //}

        //for (int i = 0; i < jobTriangles.Length; i++)
        //{
        //    triangles.Add(jobTriangles[i]);
        //}

        //jobVertices.Dispose();
        //jobTriangles.Dispose();


    }

    //Function to return a scheduled job and the heights array for the mesh data generation.
    public List<JobHandle> ScheduleChunkHeightDataGeneration(int chunkSize, int chunkHeight, float frequency, float baseTerrainHeight, float terrainHeightRange, out List<NativeList<float>> heights)
    {
        //Heights list initialiser
        heights = new List<NativeList<float>>();

        //height sub-list initialisation
        for (int i = 0; i < chunkHeight + 1; i++)
        {
            heights.Add(new NativeList<float>(Allocator.TempJob));
        }

        //List for the jobhandles
        List<JobHandle> chunkNoiseGenerationHandles = new List<JobHandle>();

        //For every layer
        for (int i = 0; i < chunkHeight + 1; i++)
        {
            //Job creation
            ChunkNoiseGenerationJob chunkNoiseGenerationJob = new ChunkNoiseGenerationJob()
            {
                chunkHeight = chunkHeight,
                chunkSize = chunkSize,
                y = i,
                frequency = frequency,
                baseTerrainHeight = baseTerrainHeight,
                terrainHeightRange = terrainHeightRange,
                result = heights[i],
            };

            //Add scheduled job to handle array
            chunkNoiseGenerationHandles.Add(chunkNoiseGenerationJob.Schedule());

        }
        //Return scheduled job array, ready for completion
        return chunkNoiseGenerationHandles;
    }

    //public JobHandle ScheduleChunkMeshDataGeneration()
    //{

    //}

    //Function to populate the terrain map array
    void PopulateTerrainMap()
    {
        //For every point on x axis
        for (int x = 0; x < width + 1; x++)
        {
            //For every point on y axis
            for (int y = 0; y < height + 1; y++)
            {
                //For every point on z axis
                for (int z = 0; z < width + 1; z++)
                {
                    //Height generation using a noise function
                    float thisHeight = MarchingData.GetTerrainHeight(x + chunkPosition.x, z + chunkPosition.z, frequency);

                    if (x > 5 && x < 11 && z > 5 && z < 11)
                    {
                        thisHeight = MarchingData.BaseTerrainHeight;
                    }
                    //Assign height into array
                    terrainMap[x, y, z] = (float)y - thisHeight;
                }
            }
        }
    }

    //Function to create the mesh data from returned perlin noise
    void CreateMeshData()
    {
        PopulateTerrainMap();
        ClearMeshData();
        //For every point on x axis
        for (int x = 0; x < width; x++)
        {
            //For every point on y axis
            for (int y = 0; y < height; y++)
            {
                //For every point on z axis
                for (int z = 0; z < width; z++)
                {
                    //March the cube
                    MarchCube(new Vector3Int(x, y, z));
                }
            }
        }
        BuildMesh();
    }


    //Function to generate the triangle table index when given the cube data
    int GetCubeConfiguration(float[] cube)
    {
        int configurationIndex = 0;
        //For every vertex in cube data
        for (int i = 0; i < 8; i++)
        {
            if (cube[i] > terrainSurface)
            {
                //Bit shift, setting index at a byte level based on the cube vertex index
                configurationIndex |= 1 << i;
            }
        }

        //Return the configuration index
        return configurationIndex;
    }

    //Function to march the cube
    void MarchCube(Vector3Int position)
    {
        float time = Time.time;

        //Create cube array
        float[] cube = new float[8];
        //For every vertex in cube
        for (int i = 0; i < 8; i++)
        {
            cube[i] = SampleTerrain(position + MarchingData.vertexTable[i]);
        }

        //Get configuration index
        int configIndex = GetCubeConfiguration(cube);

        //If index is air or underground
        if (configIndex == 0 || configIndex == 255)
        {
            //Skip this cube
            return;
        }

        int edgeIndex = 0;
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 3; j++)
            {

                //Get index from triangulation table
                int index = MarchingData.triangleTable[configIndex, edgeIndex];

                //If reached end of triangulation
                if (index == -1)
                {
                    //Return out of function
                    return;
                }

                //Generate edge vertices
                Vector3 vert1 = position + MarchingData.vertexTable[MarchingData.edgeTable[index, 0]];
                Vector3 vert2 = position + MarchingData.vertexTable[MarchingData.edgeTable[index, 1]];

                //Get vertex midpoint position from edge
                Vector3 vertPosition = (vert1 + vert2) / 2f;

                triangles.Add(VertForIndex(vertPosition));
                edgeIndex++;
            }
        }

        //UVs
        for (int z = 0; z < width; z++)
        {
            for (int x = 0; x < width; x++)
            {
                uvs.Add(new Vector2((float)x / width, (float)z / width));
            }
        }


        Debug.Log((Time.time - time) * 1000 + "ms");
    }

    public void PlaceTerrain(Vector3 pos)
    {
        Vector3Int v3Int = new Vector3Int(Mathf.CeilToInt(pos.x), Mathf.CeilToInt(pos.y), Mathf.CeilToInt(pos.z));
        terrainMap[v3Int.x, v3Int.y, v3Int.z] = 0f;

        ClearMeshData();
        CreateMeshData();
        BuildMesh();
    }

    public void RemoveTerrain(Vector3 pos)
    {
        Vector3Int v3Int = new Vector3Int(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
        terrainMap[v3Int.x, v3Int.y, v3Int.z] = 1f;

        ClearMeshData();
        CreateMeshData();
        BuildMesh();
    }

    float SampleTerrain(Vector3Int point)
    {
        return terrainMap[point.x, point.y, point.z];
    }

    int VertForIndex(Vector3 vert)
    {
        //Loop through all vertices currently in the vertices list.
        for (int i = 0; i < vertices.Count; i++)
        {
            //If a vert is found that matches passed in vert, return this index.
            if (vertices[i] == vert)
            {
                return i;
            }
        }

        //If not found, add to list and return its index
        vertices.Add(vert);
        return vertices.Count - 1;
    }

    //Function to clear mesh data lists
    void ClearMeshData()
    {
        vertices.Clear();
        triangles.Clear();
    }

    //Function to create a new mesh from mesh data lists
    void BuildMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        mesh.name = "Terrain";
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }
}

//A job to generate simplex / perlin noise efficiently
[BurstCompile]
public struct ChunkNoiseGenerationJob : IJob
{
    public float chunkSize;
    public float chunkHeight;
    public float y;
    public float frequency;
    public float baseTerrainHeight;
    public float terrainHeightRange;
    public NativeList<float> result;

    public void Execute()
    {
        for (int x = 0; x < chunkSize + 1; x++)
        {
            for (int i = 0, z = 0; z < chunkSize; z++)
            {
                result.Add(y - terrainHeightRange * noise.snoise(new float2(x * frequency / 1000, z * frequency / 1000)) + baseTerrainHeight);
                i++;
            }
        }
    }
}