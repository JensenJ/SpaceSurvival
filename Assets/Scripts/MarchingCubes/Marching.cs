using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Marching : MonoBehaviour
{
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();

    MeshFilter meshFilter;

    int configIndex = -1;

    private void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            configIndex++;
            ClearMeshData();
            MarchCube(Vector3.zero, configIndex);
            BuildMesh();
        }
    }

    void MarchCube(Vector3 position, int configIndex)
    {
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
