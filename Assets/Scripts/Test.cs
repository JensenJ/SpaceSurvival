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

    List<Vector2> points;

    private void OnValidate()
    {
        points = new List<Vector2>();
        if(radius == 0)
        {
            return;
        }
        points = PoissonDiscSampler.GenerateSample(radius, regionSize, rejectionSamples);

        Debug.Log(points.Count);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(regionSize / 2, regionSize);
        if (points != null)
        {
            foreach (Vector2 point in points)
            {
                Gizmos.DrawWireSphere(point, displayRadius);
            }
        }
    }
}
