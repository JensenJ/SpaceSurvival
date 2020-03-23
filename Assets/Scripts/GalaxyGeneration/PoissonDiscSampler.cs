using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using System.Collections.Generic;

//Class to handle poisson disc sampling, an efficient procedural generation algorithm to place objects relatively equidistant to one another, resulting in even distribution. 
public class PoissonDiscSampler : MonoBehaviour
{
    //Function to generate a simgle sample of vectors from given data
    public static List<Vector2> GenerateRingedSingleSample(float radius, Vector2 generationOffset, Vector2 sampleRegionSize,
        bool useRingPattern = false, float ringInnerRadius = 10, float ringOuterRadius = 50, int numSamplesBeforeRejection = 30)
    {
        //Debug time
        float time = Time.realtimeSinceStartup;

        //Position native list for data received from job
        NativeList<float2> positions = new NativeList<float2>(Allocator.TempJob);
        //Job creation
        DiscSamplerJob job = new DiscSamplerJob()
        {
            radius = radius,
            sampleRegionSize = new float2(sampleRegionSize.x, sampleRegionSize.y),
            numSamplesBeforeRejection = numSamplesBeforeRejection,
            generatedPositions = positions,
            randomSeed = (uint)UnityEngine.Random.Range(1, 10000),
            positionOffset = new float2(generationOffset.x, generationOffset.y),
            useRingPattern = useRingPattern,
            ringInnerRadius = ringInnerRadius,
            ringOuterRadius = ringOuterRadius,
        };

        //Job scheduling and completion
        JobHandle jobHandle = job.Schedule();
        jobHandle.Complete();

        //Creating final generation positions for function return value / output
        List<Vector2> generatedPositions = new List<Vector2>();
        for (int i = 0; i < positions.Length; i++)
        {
            //Add positions from job calculations to array
            generatedPositions.Add(positions[i]);
        }

        //Dispose of positions native array
        positions.Dispose();

        //Log debug time taken
        Debug.Log("Single Poisson Generation: " + ((Time.realtimeSinceStartup - time) * 1000f) + "ms");

        //Return generation positions
        return generatedPositions;
    }

    //Function to generate a multi-dimensional list of vectors for multi-sampling when given arrays of data to process. 
    //This function is more efficient for multisampling than just iterating over the single sample as it uses multi-threading.
    public static List<List<Vector2>> GenerateRingedMultiSample(float[] radii, Vector2[] generationOffsets, Vector2[] sampleRegionSizes,
        bool useRingPattern, float[] ringInnerRadius, float[] ringOuterRadius, int numSamplesBeforeRejection = 30)
    {
        //Debug time
        float time = Time.realtimeSinceStartup;

        //NativeList for job handles, allows multithreading, completion within single frame
        NativeList<JobHandle> handles = new NativeList<JobHandle>(Allocator.Temp);
        //2D list for calculated positions
        List<NativeList<float2>> positions = new List<NativeList<float2>>();

        //Position sub-list initialisation
        for (int i = 0; i < radii.Length; i++)
        {
            positions.Add(new NativeList<float2>(Allocator.TempJob));
        }

        //For every length of data given within array
        for (int i = 0; i < radii.Length; i++)
        {
            //Check for potential performance problems by calculating whether there are too many items to maintain high performance
            if (radii[i] < ((sampleRegionSizes[i].x * sampleRegionSizes[i].y) / 1000))
            {
                Debug.LogWarning("Poisson Disc Sampler: Radius is too small for the size of the grid, performance cannot be guaranteed!");
            }

            //Job creation
            DiscSamplerJob job = new DiscSamplerJob()
            {
                numSamplesBeforeRejection = numSamplesBeforeRejection,
                radius = radii[i],
                randomSeed = (uint)UnityEngine.Random.Range(1, 100000),
                generatedPositions = positions[i],
                sampleRegionSize = new float2(sampleRegionSizes[i]),
                positionOffset = new float2(generationOffsets[i]),
                useRingPattern = useRingPattern,
                ringInnerRadius = ringInnerRadius[i],
                ringOuterRadius = ringOuterRadius[i],
            };
            //Add scheduled job to handle list, ready for mass completion
            handles.Add(job.Schedule());
        }
        //Complete all jobs
        JobHandle.CompleteAll(handles);
        //Dispose of handles list
        handles.Dispose();

        //Create final positions 2D list
        List<List<Vector2>> finalPositions = new List<List<Vector2>>();

        for (int i = 0; i < positions.Count; i++)
        {
            //Create position sublist for every iteration
            List<Vector2> positionList = new List<Vector2>();
            for (int j = 0; j < positions[i].Length; j++)
            {
                //Add value to new sub list
                positionList.Add(positions[i][j]);
            }
            //Add new sub list to main list
            finalPositions.Add(positionList);
            //Dispose of original native sublist
            positions[i].Dispose();
        }
        
        //Debug time log
        Debug.Log("Multi Poisson Generation:" + ((Time.realtimeSinceStartup - time) * 1000f) + "ms");

        //Return positions
        return finalPositions;
    }
}


//Burst compiled job to calculate / perform the poisson disc sampling algorithm
[BurstCompile]
public struct DiscSamplerJob : IJob
{
    //Job variables
    public float radius;
    public float2 sampleRegionSize;
    public int numSamplesBeforeRejection;
    public NativeList<float2> generatedPositions;
    public float2 positionOffset;
    public uint randomSeed;
    public bool useRingPattern;
    public float ringInnerRadius;
    public float ringOuterRadius;

    //Job execution
    public void Execute()
    {
        //Calculate size of each grid
        float cellsize = radius / math.sqrt(2);

        //Array/list initalisation
        NativeArray2D<int> grid = new NativeArray2D<int>((int)math.ceil(sampleRegionSize.x / cellsize), (int)math.ceil(sampleRegionSize.y / cellsize), Allocator.Temp);
        NativeList<float2> points = new NativeList<float2>(Allocator.Temp);
        NativeList<float2> spawnPoints = new NativeList<float2>(Allocator.Temp);
        
        spawnPoints.Add(sampleRegionSize / 2);
        //Main loop
        while (spawnPoints.Length > 0)
        {
            //Create new potential position
            Unity.Mathematics.Random random = new Unity.Mathematics.Random(randomSeed);
            int spawnIndex = random.NextInt(0, spawnPoints.Length);
            float2 spawnCentre = spawnPoints[spawnIndex];

            bool candidateAccepted = false;

            //Attempts before disregarding potential position / candidate
            for (int i = 0; i < numSamplesBeforeRejection; i++)
            {
                //Actual position placement
                float angle = random.NextFloat(0, 1) * math.PI * 2;
                float2 dir = new float2(math.sin(angle), math.cos(angle));

                //Applying position to candidate
                float2 candidate = spawnCentre + dir * random.NextFloat(radius, 2 * radius);

                //Checks if position is valid (not within range of another object)
                if (IsValid(candidate, sampleRegionSize, cellsize, radius, points, grid))
                {
                    //If position is valid, add to arrays and break out of loop, ready for next potential position
                    points.Add(candidate);
                    spawnPoints.Add(candidate);
                    grid[(int)(candidate.x / cellsize), (int)(candidate.y / cellsize)] = points.Length;
                    candidateAccepted = true;
                    break;
                }
            }
            //If not accepted, remove the potential position from the spawnpoints list
            if (!candidateAccepted)
            {
                spawnPoints.RemoveAtSwapBack(spawnIndex);
            }
        }

        //Checks ring pattern
        if (useRingPattern == true)
        {
            //For every point generated so far
            for (int i = 0; i < points.Length; i++)
            {
                //Gets the distance between the middle of sample region and the candidate
                float ringDistance = math.distancesq(points[i], sampleRegionSize / 2);

                //Check if point is out of the inner radius
                if (ringDistance < ringInnerRadius * ringInnerRadius)
                {
                    //Remove point
                    points.RemoveAtSwapBack(i);
                    i--;
                }

                //Check if point is out of the outer radius
                if (ringDistance > ringOuterRadius * ringOuterRadius)
                {
                    //Remove point
                    points.RemoveAtSwapBack(i);
                    i--;
                }
            }
        }

        //For every point that is valid, add it to generated positions list with the given offset applied.
        for (int i = 0; i < points.Length; i++)
        {
            generatedPositions.Add(points[i] + positionOffset);
        }

        //Dispose of native lists / arrays
        points.Dispose();
        spawnPoints.Dispose();
        
        //Function to determine whether a candidate is within range of another one
        bool IsValid(float2 candidate, float2 sampleRegionSize, float cellSize, float radius, NativeList<float2> _points, NativeArray2D<int> _grid)
        {
            //Check whether candidate is within the actual sample region
            if(candidate.x >= 0 && candidate.x < sampleRegionSize.x && candidate.y >= 0 && candidate.y < sampleRegionSize.y)
            {
                //If candidate is, search within each cell for another candidate
                int cellX = (int)(candidate.x / cellSize);
                int cellY = (int)(candidate.y / cellSize);
                int searchStartX = math.max(0, cellX - 2);
                int searchEndX = math.min(cellX + 2, _grid.Length0 - 1);
                int searchStartY = math.max(0, cellY - 2);
                int searchEndY = Mathf.Min(cellY + 2, _grid.Length1 - 1);

                //For every x searched
                for (int x = searchStartX; x <= searchEndX; x++)
                {
                    //For every y searched
                    for (int y = searchStartY; y <= searchEndY; y++)
                    {
                        //Get the new points index
                        int pointIndex = _grid[x, y] - 1;
                        //Check point has a valid index
                        if(pointIndex != -1)
                        {
                            //Get the distance between the candidate and the new point
                            float sqrDist = math.distancesq(candidate, _points[pointIndex]);

                            //If distance is less than the given radius
                            if(sqrDist < radius * radius)
                            {
                                //This candidate is not valid
                                return false;
                            }
                        }
                    }
                }

                //This candidate is valid if it passes through all points without being in range of another
                return true;
            }
            //This candidate is not valid as it is not within the sample region.
            return false;
        }
    }
}
