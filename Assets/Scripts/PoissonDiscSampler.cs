using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Jobs;
using Unity.Burst;
using System.Collections.Generic;

public class PoissonDiscSampler : MonoBehaviour
{ 
    public static List<Vector2> GenerateSample(float radius, Vector2 sampleRegionSize, int numSamplesBeforeRejection = 30)
    {
        float time = Time.realtimeSinceStartup;

        NativeList<float2> positions = new NativeList<float2>(Allocator.TempJob);
        DiscSamplerJob job = new DiscSamplerJob()
        {
            radius = radius,
            sampleRegionSize = new float2(sampleRegionSize.x, sampleRegionSize.y),
            numSamplesBeforeRejection = numSamplesBeforeRejection,
            generatedPositions = positions,
            randomSeed = (uint)UnityEngine.Random.Range(1, 10000),
        };


        JobHandle jobHandle = job.Schedule();
        jobHandle.Complete();
        
        positions = job.generatedPositions;
        //Debug.Log("Testing job" + job.generatedPositions[0]);

        List<Vector2> generatedPositions = new List<Vector2>();
        for (int i = 0; i < positions.Length; i++)
        {
            if(positions[i].x > 0 && positions[i].y > 0)
            {
                generatedPositions.Add(positions[i]);
            }
        }

        positions.Dispose();

        Debug.Log("Poisson Generation " + (Time.realtimeSinceStartup - time) + "ms");

        return generatedPositions;
    }
}

public struct DiscSamplerJob : IJob
{
    public float radius;
    public float2 sampleRegionSize;
    public int numSamplesBeforeRejection;
    public NativeList<float2> generatedPositions;
    public uint randomSeed;

    public void Execute()
    {
        float cellsize = radius / math.sqrt(2);

        int[,] grid = new int[(int)math.ceil(sampleRegionSize.x / cellsize), (int)math.ceil(sampleRegionSize.y / cellsize)];
        NativeList<float2> points = new NativeList<float2>(Allocator.Temp);
        NativeList<float2> spawnPoints = new NativeList<float2>(Allocator.Temp);

        for (int i = 0; i < generatedPositions.Length; i++)
        {
            generatedPositions[i] = new float2(-1.0f, -1.0f);
        }

        spawnPoints.Add(sampleRegionSize / 2);
        while (spawnPoints.Length > 0)
        {
            //int spawnIndex = UnityEngine.Random.Range(0, spawnPoints.Length);
            Unity.Mathematics.Random random = new Unity.Mathematics.Random(randomSeed);
            int spawnIndex = random.NextInt(0, spawnPoints.Length);
            float2 spawnCentre = spawnPoints[spawnIndex];

            bool candidateAccepted = false;

            for (int i = 0; i < numSamplesBeforeRejection; i++)
            {
                //float angle = UnityEngine.Random.value * math.PI * 2;
                float angle = random.NextFloat(0, 1) * math.PI * 2;
                float2 dir = new float2(math.sin(angle), math.cos(angle));
                //float2 candidate = spawnCentre + dir * UnityEngine.Random.Range(radius, 2 * radius);
                float2 candidate = spawnCentre + dir * random.NextFloat(radius, 2 * radius);
                if (IsValid(candidate, sampleRegionSize, cellsize, radius, points, grid))
                {
                    points.Add(candidate);
                    spawnPoints.Add(candidate);
                    grid[(int)(candidate.x / cellsize), (int)(candidate.y / cellsize)] = points.Length;
                    candidateAccepted = true;
                    break;
                }
            }
            if (!candidateAccepted)
            {
                spawnPoints.RemoveAtSwapBack(spawnIndex);
            }
        }

        for (int i = 0; i < points.Length; i++)
        {
            generatedPositions.Add(points[i]);
            //Debug.Log(generatedPositions[i]);
        }

        points.Dispose();
        spawnPoints.Dispose();

        bool IsValid(float2 candidate, float2 sampleRegionSize, float cellSize, float radius, NativeList<float2> _points, int[,] _grid)
        {
            if(candidate.x >= 0 && candidate.x < sampleRegionSize.x && candidate.y >= 0 && candidate.y < sampleRegionSize.y)
            {
                int cellX = (int)(candidate.x / cellSize);
                int cellY = (int)(candidate.y / cellSize);
                int searchStartX = math.max(0, cellX - 2);
                int searchEndX = math.min(cellX + 2, _grid.GetLength(0) - 1);
                int searchStartY = math.max(0, cellY - 2);
                int searchEndY = Mathf.Min(cellY + 2, _grid.GetLength(1) - 1);

                for (int x = searchStartX; x <= searchEndX; x++)
                {
                    for (int y = searchStartY; y <= searchEndY; y++)
                    {
                        int pointIndex = _grid[x, y] - 1;
                        if(pointIndex != -1)
                        {
                            float sqrDist = math.distancesq(candidate, _points[pointIndex]);
                            if(sqrDist < radius * radius)
                            {
                                //_points.Dispose();
                                return false;
                            }
                        }
                    }
                }
                //_points.Dispose();
                return true;
            }
            //_points.Dispose();
            return false;
        }
    }
}
