using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class ParticlePositionMapWindow : EditorWindow
{
    Object meshInput = null;
    Vector2Int textureDimensionInput = new Vector2Int();


    [MenuItem("Window/Visual Effects/Utilities/Position Map From Mesh Tool")]
    public static void ShowWindow()
    {
        GetWindow<ParticlePositionMapWindow>("Position Map Tool");
    }

    void OnGUI()
    {
        //Mesh Input Field
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Mesh");
        meshInput = EditorGUILayout.ObjectField(meshInput, typeof(Mesh), true);
        EditorGUILayout.EndHorizontal();

        //Texture Size Input Field
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Texture Dimensions");
        textureDimensionInput = EditorGUILayout.Vector2IntField("", textureDimensionInput);
        EditorGUILayout.EndHorizontal();
        
        //Check if mesh is selected
        if(meshInput == null)
        {
            return;
        }

        //Texture dimension value validation
        if(textureDimensionInput.x <= 0 || textureDimensionInput.y <= 0 || textureDimensionInput.x > 2048 || textureDimensionInput.y > 2048)
        {
            return;
        }

        //Get actual mesh from the object
        Mesh mesh = (Mesh)meshInput;

        //Save button
        if(GUILayout.Button("Save Texture"))
        {
            //Generate the texture
            Texture2D generatedTexture = GeneratePositionMap(mesh, textureDimensionInput);

            //Bring up save menu and get selected path
            string wholePath = EditorUtility.SaveFilePanel("Save File", "Assets/", generatedTexture.name, "asset");

            //If path is valid
            if(wholePath.Length != 0)
            {
                //Find index where assets start
                int pathStart = wholePath.IndexOf("Assets");
                //If path contains "Assets"
                if (pathStart != -1)
                {
                    string assetPath = wholePath.Substring(pathStart);
                    //Create the asset
                    AssetDatabase.CreateAsset(generatedTexture, assetPath);
                }
            }
        }
    }

    //Function to generate the position map.
    Texture2D GeneratePositionMap(Mesh mesh, Vector2Int textureDimensions)
    {
        //Texture format
        TextureFormat format = TextureFormat.RGBAFloat;

        //Create texture and set wrap mode
        Texture2D positionMap = new Texture2D(textureDimensions.x, textureDimensions.y, format, false)
        {
            //Set texture settings
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
            name = mesh.name + " Position Map"
        };

        //For every pixel on the x axis
        for (int x = 0; x < textureDimensions.x; x++)
        {
            //For every pixel on the y axis
            for (int y = 0; y < textureDimensions.y; y++)
            {
                //Randomly generate pixel colour
                Color pixelColour = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), 0.0f, 1.0f);

                //Set the pixel colour
                positionMap.SetPixel(x, y, pixelColour);
            }
        }

        //Apply texture
        positionMap.Apply();

        //Return the texture
        return positionMap;
    }
}
