using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using UnityEditor;

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

    public void Generate()
    {
        galaxies = new List<List<Vector2>>();
        if (galaxyRadius == 0 || solarRadius == 0)
        {
            return;
        }
        galaxyOrigins = PoissonDiscSampler.GenerateSingleSample(galaxyRadius, new Vector2(0, 0), galaxySize, galaxyRejectionSamples);
        for (int i = 0; i < galaxyOrigins.Count; i++)
        {
            galaxies.Add(PoissonDiscSampler.GenerateSingleSample(solarRadius, galaxyOrigins[i], solarSize, solarRejectionSamples));
        }

        for (int i = 0; i < galaxyOrigins.Count; i++)
        {
            Vector2 star = galaxyOrigins[i];
            star += (solarSize / 2);
            galaxyOrigins[i] = star;
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
