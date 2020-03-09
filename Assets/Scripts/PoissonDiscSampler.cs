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
    public static List<Vector2> GenerateSingleSample(float radius, Vector2 generationOffset, Vector2 sampleRegionSize,  int numSamplesBeforeRejection = 30)
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
            positionOffset = new float2(generationOffset.x, generationOffset.y),
        };

        JobHandle jobHandle = job.Schedule();
        jobHandle.Complete();

        List<Vector2> generatedPositions = new List<Vector2>();
        for (int i = 0; i < positions.Length; i++)
        {
            generatedPositions.Add(positions[i]);
        }

        positions.Dispose();

        Debug.Log("Single Poisson Generation: " + ((Time.realtimeSinceStartup - time) * 1000f) + "ms");

        return generatedPositions;
    }

    public static List<List<Vector2>> GenerateMultiSample(float[] radii, Vector2[] generationOffsets, Vector2[] sampleRegionSizes, int numSamplesBeforeRejection = 30)
    {
        float time = Time.realtimeSinceStartup;

        NativeList<JobHandle> handles = new NativeList<JobHandle>(Allocator.Temp);
        List<NativeList<float2>> positions = new List<NativeList<float2>>();

        for (int i = 0; i < radii.Length; i++)
        {
            positions.Add(new NativeList<float2>(Allocator.TempJob));
        }

        for (int i = 0; i < radii.Length; i++)
        {
            if (radii[i] < ((sampleRegionSizes[i].x * sampleRegionSizes[i].y) / 1000))
            {
                Debug.LogWarning("Poisson Disc Sampler: Radius is too small for the size of the grid, performance cannot be guaranteed!");
            }

            DiscSamplerJob job = new DiscSamplerJob()
            {
                numSamplesBeforeRejection = numSamplesBeforeRejection,
                radius = radii[i],
                randomSeed = (uint)UnityEngine.Random.Range(1, 100000),
                generatedPositions = positions[i],
                sampleRegionSize = new float2(sampleRegionSizes[i]),
                positionOffset = new float2(generationOffsets[i]),
            };
            handles.Add(job.Schedule());
        }
        JobHandle.CompleteAll(handles);
        handles.Dispose();

        List<List<Vector2>> finalPositions = new List<List<Vector2>>();

        for (int i = 0; i < positions.Count; i++)
        {
            List<Vector2> positionList = new List<Vector2>();
            for (int j = 0; j < positions[i].Length; j++)
            {
                positionList.Add(positions[i][j]);
            }
            finalPositions.Add(positionList);
            positions[i].Dispose();
        }
        

        Debug.Log("Poisson Test Time:" + ((Time.realtimeSinceStartup - time) * 1000f) + "ms");

        return finalPositions;
    }
}

[BurstCompile]
public struct DiscSamplerJob : IJob
{
    public float radius;
    public float2 sampleRegionSize;
    public int numSamplesBeforeRejection;
    public NativeList<float2> generatedPositions;
    public float2 positionOffset;
    public uint randomSeed;

    public void Execute()
    {
        float cellsize = radius / math.sqrt(2);

        NativeArray2D<int> grid = new NativeArray2D<int>((int)math.ceil(sampleRegionSize.x / cellsize), (int)math.ceil(sampleRegionSize.y / cellsize), Allocator.Temp);
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
                float angle = random.NextFloat(0, 1) * math.PI * 2;
                float2 dir = new float2(math.sin(angle), math.cos(angle));
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
            generatedPositions.Add(points[i] + positionOffset);
        }

        points.Dispose();
        spawnPoints.Dispose();
        
        bool IsValid(float2 candidate, float2 sampleRegionSize, float cellSize, float radius, NativeList<float2> _points, NativeArray2D<int> _grid)
        {
            if(candidate.x >= 0 && candidate.x < sampleRegionSize.x && candidate.y >= 0 && candidate.y < sampleRegionSize.y)
            {
                int cellX = (int)(candidate.x / cellSize);
                int cellY = (int)(candidate.y / cellSize);
                int searchStartX = math.max(0, cellX - 2);
                int searchEndX = math.min(cellX + 2, _grid.Length0 - 1);
                int searchStartY = math.max(0, cellY - 2);
                int searchEndY = Mathf.Min(cellY + 2, _grid.Length1 - 1);

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
                                return false;
                            }
                        }
                    }
                }
                return true;
            }
            return false;
        }
    }
}
