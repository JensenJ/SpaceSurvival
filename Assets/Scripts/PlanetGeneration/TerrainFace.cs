using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainFace
{
    Mesh mesh;
    int resolution;
    Vector3 localUp;
    Vector3 axisA;
    Vector3 axisB;
    float radius;
    TerrainChunk parentChunk;
    public Planet planet;

    //These lists are going to be filled with generated data.
    public List<Vector3> vertices = new List<Vector3>();
    public List<int> triangles = new List<int>();

    //Constructor
    public TerrainFace(Mesh mesh, int resolution, Vector3 localUp, float radius, Planet planet)
    {
        //Value assigning
        this.mesh = mesh;
        this.resolution = resolution;
        this.localUp = localUp;
        this.radius = radius;
        this.planet = planet;

        //Calculating axis for use in mesh construction
        axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        axisB = Vector3.Cross(localUp, axisA);
    }

    //Function to construct a quadtree of chunks
    public void ConstructTree()
    {
        //Reset mesh data lists
        vertices.Clear();
        triangles.Clear();

        //Generate chunks
        parentChunk = new TerrainChunk(planet, null, null, localUp.normalized * planet.size, radius, 0, localUp, axisA, axisB);
        parentChunk.GenerateChildren();

        //Get chunk mesh data
        int triangleOffset = 0;
        foreach(TerrainChunk child in parentChunk.GetVisibleChildren())
        {
            (Vector3[], int[]) verticesAndTriangles = child.CalculateVerticesAndTriangles(triangleOffset);

            vertices.AddRange(verticesAndTriangles.Item1);
            triangles.AddRange(verticesAndTriangles.Item2);
            triangleOffset += verticesAndTriangles.Item1.Length;
        }

        //Reset mesh and apply new data
        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
    }

    //Function to update the quadtree of chunks
    public void UpdateTree()
    {
        //Reset mesh data lists
        vertices.Clear();
        triangles.Clear();

        parentChunk.UpdateChunk();

        //Get chunk mesh data
        int triangleOffset = 0;
        foreach (TerrainChunk child in parentChunk.GetVisibleChildren())
        {
            (Vector3[], int[]) verticesAndTriangles = (new Vector3[0], new int[0]);
            if(child.vertices == null)
            {
                verticesAndTriangles = child.CalculateVerticesAndTriangles(triangleOffset);
            }else if(child.vertices.Length == 0)
            {
                verticesAndTriangles = child.CalculateVerticesAndTriangles(triangleOffset);
            }
            else
            {
                verticesAndTriangles = (child.vertices, child.GetTrianglesWithOffset(triangleOffset));
            }

            vertices.AddRange(verticesAndTriangles.Item1);
            triangles.AddRange(verticesAndTriangles.Item2);
            triangleOffset += verticesAndTriangles.Item1.Length;
        }

        //Reset mesh and apply new data
        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
    }
}
