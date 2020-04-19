using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class ParticlePositionMapWindow : EditorWindow
{
    Object meshInput = null;


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
        
        //Check if mesh is selected
        if(meshInput == null)
        {
            return;
        }

        //Get actual mesh object
        Mesh mesh = (Mesh)meshInput;
        //Save button
        if(GUILayout.Button("Save Texture"))
        {
            //Generate the texture
            Texture2D generatedTexture = GeneratePositionMap(mesh);

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
    Texture2D GeneratePositionMap(Mesh mesh)
    {
        //Texture settings
        int size = 32;
        TextureFormat format = TextureFormat.RGBA32;
        TextureWrapMode wrapMode = TextureWrapMode.Clamp;

        //Create texture and set wrap mode
        Texture2D positionMap = new Texture2D(size, size, format, false);
        positionMap.wrapMode = wrapMode;

        //Set texture settings and apply
        positionMap.name = mesh.name + " Position Map";
        positionMap.Apply();

        //Return the 3D texture
        return positionMap;
    }
}
