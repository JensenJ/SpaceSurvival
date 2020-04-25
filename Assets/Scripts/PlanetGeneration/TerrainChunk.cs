using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunk
{
    public TerrainChunk[] children;
    public TerrainChunk parent;
    public Vector3 position;
    public float radius;
    public int detailLevel;
    public Vector3 localUp;
    public Vector3 axisA;
    public Vector3 axisB;

    //Constructor
    public TerrainChunk(TerrainChunk[] children, TerrainChunk parent, Vector3 position, float radius, int detailLevel, Vector3 localUp, Vector3 axisA, Vector3 axisB)
    {
        this.children = children;
        this.parent = parent;
        this.position = position;
        this.radius = radius;
        this.detailLevel = detailLevel;
        this.localUp = localUp;
        this.axisA = axisA;
        this.axisB = axisB;
    }

    //Function to generate the children when the level of detail is increased
    public void GenerateChildren()
    {
        //If detail level is under max level and above 0
        if (detailLevel <= 8 && detailLevel >= 0)
        {
            //If within range of the next level of detail
            if (Vector3.Distance(position.normalized * Planet.size, Planet.player.position) <= Planet.detailLevelDistances[detailLevel])
            {
                //Assign the chunks children by generating new ones with half the size
                //Detail level increased by 1
                children = new TerrainChunk[4];
                children[0] = new TerrainChunk(new TerrainChunk[0], this, position + axisA * radius / 2 + axisB * radius / 2, radius / 2, detailLevel + 1, localUp, axisA, axisB);
                children[1] = new TerrainChunk(new TerrainChunk[0], this, position + axisA * radius / 2 - axisB * radius / 2, radius / 2, detailLevel + 1, localUp, axisA, axisB);
                children[2] = new TerrainChunk(new TerrainChunk[0], this, position - axisA * radius / 2 + axisB * radius / 2, radius / 2, detailLevel + 1, localUp, axisA, axisB);
                children[3] = new TerrainChunk(new TerrainChunk[0], this, position - axisA * radius / 2 - axisB * radius / 2, radius / 2, detailLevel + 1, localUp, axisA, axisB);

                //Create grandchildren
                foreach (TerrainChunk child in children)
                {
                    child.GenerateChildren();
                }
            }
        }
    }

    //Returns the latest chunk in every branch, the ones to be renderer
    public TerrainChunk[] GetVisibleChildren()
    {
        List<TerrainChunk> toBeRendered = new List<TerrainChunk>();

        if (children.Length > 0)
        {
            foreach (TerrainChunk child in children)
            {
                toBeRendered.AddRange(child.GetVisibleChildren());
            }
        }
        else
        {
            toBeRendered.Add(this);
        }

        return toBeRendered.ToArray();
    }

    //A function to construct the mesh
    public (Vector3[], int[]) CalculateVerticesAndTriangles(int triangleOffset)
    {
        int resolution = 8;

        //Array creation
        Vector3[] vertices = new Vector3[resolution * resolution];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];
        int triIndex = 0;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = x + y * resolution;
                Vector2 percent = new Vector2(x, y) / (resolution - 1);
                Vector3 pointOnUnitCube = position + ((percent.x - 0.5f) * 2 * axisA + (percent.y - 0.5f) * 2 * axisB) * radius;
                Vector3 pointOnUnitSphere = pointOnUnitCube.normalized * Planet.size; // Inflate the cube by projected the vertices onto a sphere with the size of Planet.size
                vertices[i] = pointOnUnitSphere;

                if (x != resolution - 1 && y != resolution - 1)
                {
                    triangles[triIndex + 0] = triangleOffset + i;
                    triangles[triIndex + 1] = triangleOffset + i + resolution + 1;
                    triangles[triIndex + 2] = triangleOffset + i + resolution;

                    triangles[triIndex + 3] = triangleOffset + i;
                    triangles[triIndex + 4] = triangleOffset + i + 1;
                    triangles[triIndex + 5] = triangleOffset + i + resolution + 1;
                    triIndex += 6;
                }
            }
        }
        return (vertices, triangles);
    }
}