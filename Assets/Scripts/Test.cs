using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//Custom inspector button for generating within edit mode
[CustomEditor(typeof(Test))]
public class TestInspector : Editor
{
    
    public override void OnInspectorGUI()
    {
        //Draw default Test variables
        DrawDefaultInspector();

        //Button init
        Test test = (Test)target;
        if (GUILayout.Button("Generate"))
        {
            test.Generate();
        }
    }
}

//Actual test class
public class Test : MonoBehaviour
{
    //Galaxy spawn settings
    [Header("Galaxy Spawn Settings")]
    public float galaxyRadius = 5;
    public Vector2 galaxySize = Vector2.one;
    public int galaxyRejectionSamples = 30;
    public float galaxyDisplayRadius = 1;

    //Solar system settings
    [Header("Solar System Spawn Settings")]
    public float solarRadius = 1;
    public Vector2 solarSize = Vector2.one;
    public int solarRejectionSamples = 30;
    public float solarDisplayRadius = 0.5f;

    //Lists for keeping track of positions
    List<List<Vector2>> galaxies; //Multi generation, keeps track of the positions of stars within each galaxy
    List<Vector2> galaxyOrigins; //Single generation, keeps track of the positions of galaxies, used to calculate star / solar system positions

    //When editor refreshes
    private void OnValidate()
    {
        Generate();
    }

    //Every frame
    private void Update()
    {
        Generate();
    }

    //Function to generate a universe
    public void Generate()
    {
        //Checking values
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

        //Setting up galaxy and data, this can be used in future to add randomness such as different radii, galaxy sizes etc.
        float[] radii = new float[galaxyOrigins.Count];
        Vector2[] sampleSizes = new Vector2[galaxyOrigins.Count];
        //Data filling
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

    //Draw debug symbols for now until entity system created that handles universe generation
    private void OnDrawGizmos()
    {
        //Whole universe boundary box
        Gizmos.DrawWireCube((galaxySize + solarSize) / 2, galaxySize + solarSize);
        if (galaxies != null)
        {
            //For every galaxy
            foreach (List<Vector2> galaxy in galaxies)
            {
                //For every star / solar system
                foreach (Vector2 star in galaxy)
                {
                    //Draw the star
                    Gizmos.DrawWireSphere(star, solarDisplayRadius);
                }
            }

            //For every galaxy
            foreach (Vector2 galaxy in galaxyOrigins)
            {
                //Draw bounding box and galaxy origin
                Gizmos.DrawWireSphere(galaxy, galaxyDisplayRadius);
                Gizmos.DrawWireCube(galaxy, solarSize);
            }
        }
    }
}