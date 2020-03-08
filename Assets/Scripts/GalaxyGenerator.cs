using UnityEngine;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

public class GalaxyGenerator : MonoBehaviour
{
    //Static function to generate star positions
    public static List<Vector2> GenerateStarPositions(Vector2 galaxyOrigin)
    {
        //List initialisation 
        NativeList<float2> starPositions = new NativeList<float2>(Allocator.TempJob);

        //Job creation
        GenerateStarPositionJob job = new GenerateStarPositionJob()
        {
            positions = starPositions,
            galaxyOffset = new float2(galaxyOrigin.x, galaxyOrigin.y),
            randomSeed = (uint)UnityEngine.Random.Range(1, 10000),
            starCount = 10,
        };

        //Job scheduling
        JobHandle handle = job.Schedule();
        handle.Complete();

        List<Vector2> finalStarPositions = new List<Vector2>();

        starPositions = job.positions;

        for (int i = 0; i < starPositions.Length; i++)
        {
            finalStarPositions.Add(starPositions[i]);
        }
        //Dispose of lists
        starPositions.Dispose();

        //Return all star positions
        return finalStarPositions;
    }
}

[BurstCompile]
public struct GenerateStarPositionJob : IJob
{
    public NativeList<float2> positions;
    public float2 galaxyOffset;
    public uint randomSeed;
    public int starCount;

    public void Execute()
    {
        Unity.Mathematics.Random random = new Unity.Mathematics.Random(randomSeed);

        for (int i = 0; i < starCount; i++)
        {
            float angle = random.NextFloat(0.0f, 1.0f) * Mathf.PI * 2f;
            //Vector2 newPos = new Vector2(Mathf.Cos(angle) * (ringRadius + systemCenterRadius) + Random.Range(-systemRingCount, systemRingWidth),
            //    Mathf.Sin(angle) * (ringRadius + systemCenterRadius) + random.Range(-systemRingWidth, systemRingWidth));

            float2 newPos = new float2(math.cos(angle), math.sin(angle)) + galaxyOffset;

            positions.Add(newPos);
        }
    }
}