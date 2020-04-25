using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Transforms;
using UnityEngine;

public class TerrainChunk
{
    public uint hashvalue;
    public Planet planet;
    public TerrainFace terrainFace;
    public TerrainChunk[] children;
    public TerrainChunk parent;
    public Vector3 position;
    public float radius;
    public byte detailLevel;
    public Vector3 localUp;
    public Vector3 axisA;
    public Vector3 axisB;
    public byte corner;

    public Vector3[] vertices;
    public int[] triangles;

    public byte[] neighbours = new byte[4]; // East, west, north, south

    //Constructor
    public TerrainChunk(uint hashvalue, Planet planet, TerrainFace terrainFace, TerrainChunk[] children, Vector3 position, float radius, byte detailLevel, Vector3 localUp, Vector3 axisA, Vector3 axisB, byte[] neighbours, byte corner)
    {
        this.hashvalue = hashvalue;
        this.planet = planet;
        this.terrainFace = terrainFace;
        this.children = children;
        this.position = position;
        this.radius = radius;
        this.detailLevel = detailLevel;
        this.localUp = localUp;
        this.axisA = axisA;
        this.axisB = axisB;
        this.neighbours = neighbours;
        this.corner = corner;
    }

    //Function to generate the children when the level of detail is increased
    public void GenerateChildren()
    {
        //If detail level is under max level and above 0
        if (detailLevel <= planet.detailLevelDistances.Length - 1 && detailLevel >= 0)
        {
            //If within range of the next level of detail
            if (Vector3.Distance(planet.transform.TransformDirection(position.normalized * planet.size) + planet.transform.position, planet.player.position) <= planet.detailLevelDistances[detailLevel])
            {
                //Assign the chunks children by generating new ones with half the size
                //Detail level increased by 1
                children = new TerrainChunk[4];
                children[0] = new TerrainChunk(hashvalue * 4, planet, terrainFace, new TerrainChunk[0], position + axisA * radius / 2 - axisB * radius / 2, radius / 2, (byte)(detailLevel + 1), localUp, axisA, axisB, new byte[4], 0); // TOP LEFT
                children[1] = new TerrainChunk(hashvalue * 4 + 1, planet, terrainFace, new TerrainChunk[0], position + axisA * radius / 2 + axisB * radius / 2, radius / 2, (byte)(detailLevel + 1), localUp, axisA, axisB, new byte[4], 1); // TOP RIGHT
                children[2] = new TerrainChunk(hashvalue * 4 + 2, planet, terrainFace, new TerrainChunk[0], position - axisA * radius / 2 + axisB * radius / 2, radius / 2, (byte)(detailLevel + 1), localUp, axisA, axisB, new byte[4], 2); // BOTTOM RIGHT
                children[3] = new TerrainChunk(hashvalue * 4 + 3, planet, terrainFace, new TerrainChunk[0], position - axisA * radius / 2 - axisB * radius / 2, radius / 2, (byte)(detailLevel + 1), localUp, axisA, axisB, new byte[4], 3); // BOTTOM LEFT

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
        if (detailLevel <= planet.detailLevelDistances.Length - 1)
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

    public void GetNeighbourLOD()
    {
        neighbours = new byte[4];

        if (corner == 0) // Top left
        {
            neighbours[1] = CheckNeighbourLOD(1, hashvalue); // West
            neighbours[2] = CheckNeighbourLOD(2, hashvalue); // North
        }
        else if (corner == 1) // Top right
        {
            neighbours[0] = CheckNeighbourLOD(0, hashvalue); // East
            neighbours[2] = CheckNeighbourLOD(2, hashvalue); // North
        }
        else if (corner == 2) // Bottom right
        {
            neighbours[0] = CheckNeighbourLOD(0, hashvalue); // East
            neighbours[3] = CheckNeighbourLOD(3, hashvalue); // South
        }
        else if (corner == 3) // Bottom left
        {
            neighbours[1] = CheckNeighbourLOD(1, hashvalue); // West
            neighbours[3] = CheckNeighbourLOD(3, hashvalue); // South
        }
    }

    // Find neighbouring chunks by applying a partial inverse bitmask to the hash
    private byte CheckNeighbourLOD(byte side, uint hash)
    {
        uint bitmask = 0;
        byte count = 0;
        uint twoLast;

        while (count < detailLevel * 2) // 0 through 3 can be represented as a two bit number
        {
            count += 2;
            twoLast = (hash & 3); // Get the two last bits of the hash. 0b_10011 --> 0b_11

            bitmask = bitmask * 4; // Add zeroes to the end of the bitmask. 0b_10011 --> 0b_1001100

            // Create mask to get the quad on the opposite side. 2 = 0b_10 and generates the mask 0b_11 which flips it to 1 = 0b_01
            if (side == 2 || side == 3)
            {
                bitmask += 3; // Add 0b_11 to the bitmask
            }
            else
            {
                bitmask += 1; // Add 0b_01 to the bitmask
            }

            // Break if the hash goes in the opposite direction
            if ((side == 0 && (twoLast == 0 || twoLast == 3)) ||
                (side == 1 && (twoLast == 1 || twoLast == 2)) ||
                (side == 2 && (twoLast == 3 || twoLast == 2)) ||
                (side == 3 && (twoLast == 0 || twoLast == 1)))
            {
                break;
            }

            // Remove already processed bits. 0b_1001100 --> 0b_10011
            hash = hash >> 2;
        }

        // Return 1 (true) if the quad in quadstorage is less detailed
        if (terrainFace.parentChunk.GetQuadDetailLevel(hashvalue ^ bitmask, detailLevel) < detailLevel)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }

    // Find the detail level of the neighbouring quad using the querryHash as a map
    public byte GetQuadDetailLevel(uint querryHash, byte dl)
    {
        byte dlResult = 0; // dl = detail level

        if (hashvalue == querryHash)
        {
            dlResult = detailLevel;
        }
        else
        {
            if (children.Length > 0)
            {
                dlResult += children[((querryHash >> ((dl - 1) * 2)) & 3)].GetQuadDetailLevel(querryHash, (byte)(dl - 1));
            }
        }

        return dlResult; // Returns 0 if no quad with the given hash is found
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
        //Array creation
        Matrix4x4 transformMatrix;
        Vector3 rotationMatrixAttrib = new Vector3(0, 0, 0);
        Vector3 flipMatrixAttrib = new Vector3(1, 1, 1);
        Vector3 scaleMatrixAttrib = new Vector3(radius, radius, 1);

        //Adjust rotation according to side of planet
        if(terrainFace.localUp == Vector3.forward)
        {
            rotationMatrixAttrib = new Vector3(0, 0, 180);
        }
        else if(terrainFace.localUp == Vector3.back)
        {
            rotationMatrixAttrib = new Vector3(0, 180, 0);
        }
        else if (terrainFace.localUp == Vector3.right)
        {
            rotationMatrixAttrib = new Vector3(0, 90, 270);
        }
        else if (terrainFace.localUp == Vector3.left)
        {
            rotationMatrixAttrib = new Vector3(0, 270, 270);
        }
        else if (terrainFace.localUp == Vector3.up)
        {
            rotationMatrixAttrib = new Vector3(270, 0, 90);
        }
        else if (terrainFace.localUp == Vector3.down)
        {
            rotationMatrixAttrib = new Vector3(90, 0, 270);
        }

        //Create transform matrix
        transformMatrix = Matrix4x4.TRS(position, Quaternion.Euler(rotationMatrixAttrib), scaleMatrixAttrib);
        //Index of quad template
        int quadIndex = (neighbours[0] | neighbours[1] * 2 | neighbours[2] * 4 | neighbours[3] * 8);

        //Choose a quad from the templates, then move it using the transform matrix, normalise its vertices, scale it and store it
        vertices = new Vector3[(PlanetPresets.quadRes + 1) * (PlanetPresets.quadRes + 1)];

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = transformMatrix.MultiplyPoint(PlanetPresets.quadTemplateVertices[quadIndex][i]).normalized * planet.size;
        }
        triangles = PlanetPresets.quadTemplateTriangles[quadIndex];

        return (vertices, GetTrianglesWithOffset(triangleOffset));
    }
}