using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;

public class Test : MonoBehaviour
{
    public float radius;
    public Vector2 regionSize = Vector2.one;
    public int rejectionSamples = 30;
    public float displayRadius = 1;

    List<List<Vector2>> galaxies;

    private void OnValidate()
    {
        galaxies = new List<List<Vector2>>();
        if(radius == 0)
        {
            return;
        }
        List<Vector2> galaxyOrigins = PoissonDiscSampler.GenerateSample(radius, regionSize, rejectionSamples);
        for (int i = 0; i < galaxyOrigins.Count; i++)
        { 
            galaxies.Add(GalaxyGenerator.GenerateStarPositions(galaxyOrigins[i]));
        }
        Debug.Log(galaxies.Count);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(regionSize / 2, regionSize);
        if (galaxies != null)
        {
            foreach (List<Vector2> galaxy in galaxies)
            {
                foreach (Vector2 star in galaxy)
                {
                    Gizmos.DrawWireSphere(star, displayRadius);
                }
            }
        }
    }
}
