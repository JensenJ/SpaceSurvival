using UnityEngine;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

public class GalaxyGenerator : MonoBehaviour
{
    //Static function to generate star positions
    public static List<List<Vector2>> GenerateStarPositions(List<Vector2> galaxyOrigins)
    {
        //List initialisation 
        NativeList<GenerateStarPositionJob> generationJobs = new NativeList<GenerateStarPositionJob>(Allocator.Temp);
        NativeList<JobHandle> generationHandles = new NativeList<JobHandle>(Allocator.Temp);
        NativeList<float2> starPositions = new NativeList<float2>(Allocator.TempJob);

        //For all galaxy origins given, basically how many galaxies to generate
        for (int i = 0; i < galaxyOrigins.Count; i++)
        {
            //Job creation
            GenerateStarPositionJob job = new GenerateStarPositionJob()
            {
                positions = starPositions,
                galaxyOffset = new float2(galaxyOrigins[i].x, galaxyOrigins[i].y),
            };
            //Job scheduling
            generationHandles.Add(job.Schedule());
        }

        //Complete all galaxy generations
        JobHandle.CompleteAll(generationHandles);

        //2D list initialisation for return
        List<List<Vector2>> finalStarPositionsForAllGalaxies = new List<List<Vector2>>();

        //Add star positions to lists
        for (int i = 0; i < generationJobs.Length; i++)
        {
            //Create sub list for each galaxy generation
            List<Vector2> finalStarPositions = new List<Vector2>();
            for (int j = 0; j < generationJobs[i].positions.Length; j++)
            {
                finalStarPositions.Add(generationJobs[i].positions[j]);
            }
            finalStarPositionsForAllGalaxies.Add(finalStarPositions);
        }

        //Dispose of lists
        starPositions.Dispose();
        generationJobs.Dispose();
        generationHandles.Dispose();

        //Return all star positions for all galaxies
        return finalStarPositionsForAllGalaxies;
    }
}

[BurstCompile]
public struct GenerateStarPositionJob : IJob
{
    public NativeList<float2> positions;
    public float2 galaxyOffset;

    public void Execute()
    {
        
    }
}