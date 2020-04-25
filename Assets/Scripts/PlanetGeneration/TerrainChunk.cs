using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunk
{
    public Planet planet;
    public TerrainChunk[] children;
    public TerrainChunk parent;
    public Vector3 position;
    public float radius;
    public int detailLevel;
    public Vector3 localUp;
    public Vector3 axisA;
    public Vector3 axisB;

    public Vector3[] vertices;
    public int[] triangles;

    //Constructor
    public TerrainChunk(Planet planet, TerrainChunk[] children, TerrainChunk parent, Vector3 position, float radius, int detailLevel, Vector3 localUp, Vector3 axisA, Vector3 axisB)
    {
        this.planet = planet;
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
        int maxDetail = 8;
        //If detail level is under max level and above 0
        if (detailLevel <= maxDetail && detailLevel >= 0)
        {
            //If within range of the next level of detail
            if (Vector3.Distance(planet.transform.TransformDirection(position.normalized * planet.size) + planet.transform.position, planet.player.position) <= planet.detailLevelDistances[detailLevel])
            {
                //Assign the chunks children by generating new ones with half the size
                //Detail level increased by 1
                children = new TerrainChunk[4];
                children[0] = new TerrainChunk(planet, new TerrainChunk[0], this, position + axisA * radius / 2 + axisB * radius / 2, radius / 2, detailLevel + 1, localUp, axisA, axisB);
                children[1] = new TerrainChunk(planet, new TerrainChunk[0], this, position + axisA * radius / 2 - axisB * radius / 2, radius / 2, detailLevel + 1, localUp, axisA, axisB);
                children[2] = new TerrainChunk(planet, new TerrainChunk[0], this, position - axisA * radius / 2 + axisB * radius / 2, radius / 2, detailLevel + 1, localUp, axisA, axisB);
                children[3] = new TerrainChunk(planet, new TerrainChunk[0], this, position - axisA * radius / 2 - axisB * radius / 2, radius / 2, detailLevel + 1, localUp, axisA, axisB);

                //Create grandchildren
                foreach (TerrainChunk child in children)
                {
                    child.GenerateChildren();
                }
            }
        }
    }

    //Update the chunk
    public void UpdateChunk()
    {
        float distanceToPlayer = Vector3.Distance(planet.transform.TransformDirection(position.normalized * planet.size) + planet.transform.position, planet.player.position);
        if (detailLevel <= 8)
        {
            if(distanceToPlayer > planet.detailLevelDistances[detailLevel])
            {
                children = new TerrainChunk[0];
            }
            else
            {
                if(children.Length > 0)
                {
                    foreach(TerrainChunk child in children)
                    {
                        child.UpdateChunk();
                    }
                }
                else
                {
                    GenerateChildren();
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
            //If within viewing of the camera
            if (Mathf.Acos((Mathf.Pow(planet.size, 2) + Mathf.Pow(planet.distanceToPlayer, 2) -
               Mathf.Pow(Vector3.Distance(planet.transform.TransformDirection(position.normalized * planet.size) + planet.transform.position, planet.player.position), 2)) /
               (2 * planet.size * planet.distanceToPlayer)) < planet.cullingMinAngle)
            {
                toBeRendered.Add(this);
            }
        }

        return toBeRendered.ToArray();
    }

    //Return triangles including offset
    public int[] GetTrianglesWithOffset(int triangleOffset)
    {
        int[] triangles = new int[this.triangles.Length];

        for (int i = 0; i < triangles.Length; i++)
        {
            triangles[i] = this.triangles[i] + triangleOffset;
        }

        return triangles;
    }

    //A function to construct the mesh
    public (Vector3[], int[]) CalculateVerticesAndTriangles(int triangleOffset)
    {
        int resolution = 9; // This number must be odd

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
                Vector3 pointOnUnitCube = position + ((percent.x - 0.5f) * axisA + (percent.y - 0.5f) * axisB) * 2 * radius;
                Vector3 pointOnUnitSphere = pointOnUnitCube.normalized * planet.size; // Inflate the cube by projected the vertices onto a sphere with the size of the planet
                vertices[i] = pointOnUnitSphere;

                if (x < resolution - 1 && y < resolution - 1)
                {
                    triangles[triIndex + 0] = i;
                    triangles[triIndex + 1] = i + resolution + 1;
                    triangles[triIndex + 2] = i + resolution;

                    triangles[triIndex + 3] = i;
                    triangles[triIndex + 4] = i + 1;
                    triangles[triIndex + 5] = i + resolution + 1;
                    triIndex += 6;
                }
            }
        }
        this.vertices = vertices;
        this.triangles = triangles;

        return (vertices, GetTrianglesWithOffset(triangleOffset));
    }
}