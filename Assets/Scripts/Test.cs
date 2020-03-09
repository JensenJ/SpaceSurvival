using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using UnityEditor;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;

[CustomEditor(typeof(Test))]
public class TestInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Test test = (Test)target;
        if (GUILayout.Button("Generate"))
        {
            test.Generate();
        }
    }
}

public class Test : MonoBehaviour
{
    [Header("Galaxy Spawn Settings")]
    public float galaxyRadius = 5;
    public Vector2 galaxySize = Vector2.one;
    public int galaxyRejectionSamples = 30;
    public float galaxyDisplayRadius = 1;

    [Header("Solar System Spawn Settings")]
    public float solarRadius = 1;
    public Vector2 solarSize = Vector2.one;
    public int solarRejectionSamples = 30;
    public float solarDisplayRadius = 0.5f;

    List<List<Vector2>> galaxies;
    List<Vector2> galaxyOrigins;

    private void OnValidate()
    {
        Generate();
    }

    private void Update()
    {
        Generate();
    }

    public void Generate()
    {
        //Checking values
        galaxies = new List<List<Vector2>>();
        if (galaxyRadius == 0 || solarRadius == 0)
        {
            return;
        }

        //Galaxy origin positions
        galaxyOrigins = PoissonDiscSampler.GenerateSingleSample(galaxyRadius, new Vector2(0, 0), galaxySize, galaxyRejectionSamples);

        //Galaxy offset to keep it within boundaries
        //For every galaxy
        for (int i = 0; i < galaxyOrigins.Count; i++)
        {
            //Get galaxy origin position
            Vector2 galaxy = galaxyOrigins[i];
            //Apply galaxy offset
            galaxy += (solarSize / 2);
            galaxyOrigins[i] = galaxy;
        }

        //Setting up galaxy spawn positions and data
        float[] radii = new float[galaxyOrigins.Count];
        Vector2[] sampleSizes = new Vector2[galaxyOrigins.Count];
        for (int i = 0; i < radii.Length; i++)
        {
            radii[i] = solarRadius;
            sampleSizes[i] = solarSize;
        }

        //Generate multiple galaxy samples from an array
        galaxies = PoissonDiscSampler.GenerateMultiSample(radii, galaxyOrigins.ToArray(), sampleSizes, solarRejectionSamples);

        //Stars within each galaxy offset to fit within galaxy boundaries.
        //For each galaxy
        for (int i = 0; i < galaxies.Count; i++)
        {
            //For each star
            for (int j = 0; j < galaxies[i].Count; j++)
            {
                //Get star
                Vector2 star = galaxies[i][j];
                //Apply offset to star
                star += (solarSize / 2) - solarSize;
                //Reassign star into array
                galaxies[i][j] = star;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube((galaxySize + solarSize) / 2, galaxySize + solarSize);
        if (galaxies != null)
        {
            foreach (List<Vector2> galaxy in galaxies)
            {
                foreach (Vector2 star in galaxy)
                {
                    Gizmos.DrawWireSphere(star, solarDisplayRadius);
                }
            }

            foreach (Vector2 galaxy in galaxyOrigins)
            {
                Gizmos.DrawWireSphere(galaxy, galaxyDisplayRadius);
                Gizmos.DrawWireCube(galaxy, solarSize);
            }
        }
    }
}