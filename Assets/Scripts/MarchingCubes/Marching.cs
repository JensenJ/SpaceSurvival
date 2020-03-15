using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//A class to generate a mesh from cube data
public class Marching : MonoBehaviour
{
    //Mesh data
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    MeshFilter meshFilter;

    //Terrain variables
    float terrainSurface = 0.5f;
    int height = 8;
    int width = 32;
    float[,,] terrainMap;
    public float frequency;
    public bool smoothTerrain = false;
    public bool flatShading = false;

    //Start function
    private void Start()
    {
        //Create arrays and get components
        meshFilter = GetComponent<MeshFilter>();
        terrainMap = new float[width + 1, height + 1, width + 1];
        PopulateTerrainMap();
        CreateMeshData();
        BuildMesh();
        
    }

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
                    float thisHeight = (float)height * Mathf.PerlinNoise((float)x * (frequency / 1000f), (float)z * (frequency / 1000f));

                    //Assign height into array
                    terrainMap[x, y, z] = (float)y - thisHeight;
                }
            }
        }
    }

    //Function to create the mesh data from returned perlin noise
    void CreateMeshData()
    {
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
    }


    //Function to generate the triangle table index when given the cube data
    int GetCubeConfiguration(float[] cube)
    {
        int configurationIndex = 0;
        //For every vertex in cube data
        for (int i = 0; i < 8; i++)
        {
            if(cube[i] > terrainSurface)
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

                Vector3 vertPosition;
                if (smoothTerrain)
                {
                    //Get the terrain values at either end of the edge
                    float vert1Sample = cube[MarchingData.edgeTable[index, 0]];
                    float vert2Sample = cube[MarchingData.edgeTable[index, 1]];

                    //Calculate the difference between the two terrain values. 
                    float difference = vert2Sample - vert1Sample;
                    //If difference is 0, then the terrain passes through middle.
                    if(difference == 0)
                    {
                        difference = terrainSurface;
                    }
                    else
                    {
                        difference = (terrainSurface - vert1Sample) / difference;
                    }
                    //Calculate the point along the edge that the terrain passes through.
                    vertPosition = vert1 + ((vert2 - vert1) * difference);
                }
                else
                {
                    //Get vertex midpoint position from edge
                    vertPosition = (vert1 + vert2) / 2f;

                }

                //Add generated data to mesh data lists
                if (flatShading)
                {
                    vertices.Add(vertPosition);
                    triangles.Add(vertices.Count - 1);

                }
                else
                {
                    triangles.Add(VertForIndex(vertPosition));
                }
                edgeIndex++;
            }
        }
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
            if(vertices[i] == vert)
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
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
    }
}
