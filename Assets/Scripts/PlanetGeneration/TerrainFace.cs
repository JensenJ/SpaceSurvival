using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class TerrainFace
{
    public volatile Mesh mesh;
    public Vector3 localUp;
    Vector3 axisA;
    Vector3 axisB;
    float radius;
    public TerrainChunk parentChunk;
    public Planet planetScript;
    public List<TerrainChunk> visibleChildren = new List<TerrainChunk>();
    public bool devMode = false;

    // These will be filled with the generated data
    public List<Vector3> vertices = new List<Vector3>();
    public List<Vector3> borderVertices = new List<Vector3>();
    public List<Vector3> normals = new List<Vector3>();
    public List<int> triangles = new List<int>();
    public List<int> borderTriangles = new List<int>();
    public Dictionary<int, bool> edgefanIndex = new Dictionary<int, bool>();

    // Constructor
    public TerrainFace(Mesh mesh, Vector3 localUp, float radius, Planet planetScript, bool devMode = false)
    {
        this.mesh = mesh;
        this.localUp = localUp;
        this.radius = radius;
        this.planetScript = planetScript;
        this.devMode = devMode;

        axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        axisB = Vector3.Cross(localUp, axisA);
    }

    // Construct a quadtree of chunks (even though the chunks end up 3D, they start out 2D in the quadtree and are later projected onto a sphere)
    public void ConstructTree()
    {
        // Resets the mesh
        vertices.Clear();
        triangles.Clear();
        normals.Clear();
        borderVertices.Clear();
        borderTriangles.Clear();
        visibleChildren.Clear();

        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // Extend the resolution capabilities of the mesh

        // Generate chunks
        parentChunk = new TerrainChunk(1, planetScript, this, null, localUp.normalized * planetScript.size, radius, 0, localUp, axisA, axisB, new byte[4], 0, devMode);
        parentChunk.GenerateChildren();

        // Get chunk mesh data
        int triangleOffset = 0;
        int borderTriangleOffset = 0;
        parentChunk.GetVisibleChildren();
        foreach (TerrainChunk child in visibleChildren)
        {
            child.GetNeighbourLOD();
            (Vector3[], int[], int[], Vector3[], Vector3[]) verticesAndTriangles = child.CalculateVerticesAndTriangles(triangleOffset, borderTriangleOffset);

            vertices.AddRange(verticesAndTriangles.Item1);
            triangles.AddRange(verticesAndTriangles.Item2);
            borderTriangles.AddRange(verticesAndTriangles.Item3);
            borderVertices.AddRange(verticesAndTriangles.Item4);
            normals.AddRange(verticesAndTriangles.Item5);
            triangleOffset += verticesAndTriangles.Item1.Length;
            borderTriangleOffset += verticesAndTriangles.Item4.Length;
        }

        // Reset mesh and apply new data
        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
    }

    // Update the quadtree
    public void UpdateTree()
    {
        // Resets the mesh
        vertices.Clear();
        triangles.Clear();
        normals.Clear();
        borderVertices.Clear();
        borderTriangles.Clear();
        visibleChildren.Clear();
        edgefanIndex.Clear();

        parentChunk.UpdateChunk();

        // Get chunk mesh data
        int triangleOffset = 0;
        int borderTriangleOffset = 0;
        parentChunk.GetVisibleChildren();
        foreach (TerrainChunk child in visibleChildren)
        {
            child.GetNeighbourLOD();
            (Vector3[], int[], int[], Vector3[], Vector3[]) verticesAndTriangles = (new Vector3[0], new int[0], new int[0], new Vector3[0], new Vector3[0]);
            if (child.vertices == null)
            {
                verticesAndTriangles = child.CalculateVerticesAndTriangles(triangleOffset, borderTriangleOffset);
            }
            else if (child.vertices.Length == 0 || child.triangles != PlanetPresets.quadTemplateTriangles[(child.neighbours[0] | child.neighbours[1] * 2 | child.neighbours[2] * 4 | child.neighbours[3] * 8)])
            {
                verticesAndTriangles = child.CalculateVerticesAndTriangles(triangleOffset, borderTriangleOffset);
            }
            else
            {
                verticesAndTriangles = (child.vertices, child.GetTrianglesWithOffset(triangleOffset), child.GetBorderTrianglesWithOffset(borderTriangleOffset, triangleOffset), child.borderVertices, child.normals);
            }

            vertices.AddRange(verticesAndTriangles.Item1);
            triangles.AddRange(verticesAndTriangles.Item2);
            borderTriangles.AddRange(verticesAndTriangles.Item3);
            borderVertices.AddRange(verticesAndTriangles.Item4);
            normals.AddRange(verticesAndTriangles.Item5);

            // Increase offset to accurately point to the next slot in the lists
            triangleOffset += (PlanetPresets.quadRes + 1) * (PlanetPresets.quadRes + 1);
            borderTriangleOffset += verticesAndTriangles.Item4.Length;
        }

        Vector2[] uvs = new Vector2[vertices.Count];

        float planetScriptSizeDivide = (1 / planetScript.size);
        float twoPiDivide = (1 / (2 * Mathf.PI));

        for (int i = 0; i < uvs.Length; i++)
        {
            Vector3 d = vertices[i] * planetScriptSizeDivide;
            float u = 0.5f + Mathf.Atan2(d.z, d.x) * twoPiDivide;
            float v = 0.5f - Mathf.Asin(d.y) / Mathf.PI;

            uvs[i] = new Vector2(u, v);
        }

        // Reset mesh and apply new data
        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uvs;
    }
}