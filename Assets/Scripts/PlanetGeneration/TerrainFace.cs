using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainFace
{
    Mesh mesh;
    public Vector3 localUp;
    Vector3 axisA;
    Vector3 axisB;
    float radius;
    public TerrainChunk parentChunk;
    public Planet planetScript;

    // These will be filled with the generated data
    public List<Vector3> vertices = new List<Vector3>();
    public List<int> triangles = new List<int>();

    // Constructor
    public TerrainFace(Mesh mesh, Vector3 localUp, float radius, Planet planetScript)
    {
        this.mesh = mesh;
        this.localUp = localUp;
        this.radius = radius;
        this.planetScript = planetScript;

        axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        axisB = Vector3.Cross(localUp, axisA);
    }

    // Construct a quadtree of chunks (even though the chunks end up 3D, they start out 2D in the quadtree and are later projected onto a sphere)
    public void ConstructTree()
    {
        // Resets the mesh
        vertices.Clear();
        triangles.Clear();

        // Generate chunks
        parentChunk = new TerrainChunk(1, planetScript, this, null, localUp.normalized * planetScript.size, radius, 0, localUp, axisA, axisB, new byte[4], 0);
        parentChunk.GenerateChildren();

        // Get chunk mesh data
        int triangleOffset = 0;
        foreach (TerrainChunk child in parentChunk.GetVisibleChildren())
        {
            child.GetNeighbourLOD();
            (Vector3[], int[]) verticesAndTriangles = child.CalculateVerticesAndTriangles(triangleOffset);

            vertices.AddRange(verticesAndTriangles.Item1);
            triangles.AddRange(verticesAndTriangles.Item2);
            triangleOffset += verticesAndTriangles.Item1.Length;
        }

        // Reset mesh and apply new data
        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
    }

    // Update the quadtree
    public void UpdateTree()
    {
        // Resets the mesh
        vertices.Clear();
        triangles.Clear();

        parentChunk.UpdateChunk();

        // Get chunk mesh data
        int triangleOffset = 0;
        foreach (TerrainChunk child in parentChunk.GetVisibleChildren())
        {
            child.GetNeighbourLOD();
            (Vector3[], int[]) verticesAndTriangles = (new Vector3[0], new int[0]);
            if (child.vertices == null)
            {
                verticesAndTriangles = child.CalculateVerticesAndTriangles(triangleOffset);
            }
            else if (child.vertices.Length == 0 || child.triangles != PlanetPresets.quadTemplateTriangles[(child.neighbours[0] | child.neighbours[1] * 2 | child.neighbours[2] * 4 | child.neighbours[3] * 8)])
            {
                verticesAndTriangles = child.CalculateVerticesAndTriangles(triangleOffset);
            }
            else//Check if neighbour LODS are the same or not
            {
                verticesAndTriangles = (child.vertices, child.GetTrianglesWithOffset(triangleOffset));
            }

            vertices.AddRange(verticesAndTriangles.Item1);
            triangles.AddRange(verticesAndTriangles.Item2);
            triangleOffset += verticesAndTriangles.Item1.Length;
        }

        // Reset mesh and apply new data
        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
    }
}