using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Marching : MonoBehaviour
{
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();

    MeshFilter meshFilter;

    float terrainSurface = 0.5f;
    int width = 32;
    int height = 8;
    float[,,] terrainMap;

    int configIndex = -1;

    private void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        terrainMap = new float[width + 1, height + 1, width + 1];
        PopulateTerrainMap();
        CreateMeshData();
        BuildMesh();
    }

    void CreateMeshData()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < width; z++)
                {
                    float[] cube = new float[8];
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3Int corner = new Vector3Int(x, y, z) + MarchingData.vertexTable[i];
                        cube[i] = terrainMap[corner.x, corner.y, corner.z];
                    }

                    MarchCube(new Vector3(x, y, z), cube);
                }
            }
        }
    }

    void PopulateTerrainMap()
    {
        for (int x = 0; x < width + 1; x++)
        {
            for (int y = 0; y < height + 1; y++)
            {
                for (int z = 0; z < width + 1; z++)
                {
                    float thisHeight = (float)height * Mathf.PerlinNoise((float)x / 16f * 1.5f + 0.001f, (float)z / 16f * 1.5f + 0.001f);

                    float point;

                    if(y <= thisHeight - 0.5f)
                    {
                        point = 0f;
                    }
                    else if(y > thisHeight + 0.5f)
                    {
                        point = 1f;
                    }
                    else if(y > thisHeight)
                    {
                        point = (float)y - thisHeight;
                    }
                    else
                    {
                        point = thisHeight - (float)y;
                    }

                    terrainMap[x, y, z] = point;
                }
            }
        }
    }

    int GetCubeConfiguration(float[] cube)
    {
        int configurationIndex = 0;
        for (int i = 0; i < 8; i++)
        {
            if(cube[i] > terrainSurface)
            {
                configurationIndex |= 1 << i;
            }
        }

        return configurationIndex;
    }

    void MarchCube(Vector3 position, float[] cube)
    {
        int configIndex = GetCubeConfiguration(cube);

        if (configIndex == 0 || configIndex == 255)
        {
            return;
        }

        int edgeIndex = 0;
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 3; j++)
            {

                int index = MarchingData.triangleTable[configIndex, edgeIndex];

                if (index == -1)
                {
                    return;
                }


                Vector3 vert1 = position + MarchingData.edgeTable[index, 0];
                Vector3 vert2 = position + MarchingData.edgeTable[index, 1];

                Vector3 vertPosition = (vert1 + vert2) / 2f;

                vertices.Add(vertPosition);
                triangles.Add(vertices.Count - 1);
                edgeIndex++;
            }
        }
    }

    void ClearMeshData()
    {
        vertices.Clear();
        triangles.Clear();
    }

    void BuildMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
    }
}
