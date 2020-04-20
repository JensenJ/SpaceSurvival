using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.VFX;
using JUCL.Utilities;


public class ParticlePositionMapWindow : EditorWindow
{
    Object meshInput = null;
    Object materialInput = null;
    Vector2Int textureDimensionInput = new Vector2Int();
    
    [MenuItem("Window/Visual Effects/Utilities/Position Map From Mesh")]
    public static void ShowWindow()
    {
        GetWindow<ParticlePositionMapWindow>("Position Map Tool");
    }

    void OnGUI()
    {
        //Mesh Input Field
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Mesh");
        meshInput = EditorGUILayout.ObjectField(meshInput, typeof(GameObject), true);
        EditorGUILayout.EndHorizontal();

        //Material Input field
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Material");
        materialInput = EditorGUILayout.ObjectField(materialInput, typeof(Material), true);
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

        //Check if material is selected
        if(materialInput == null)
        {
            return;
        }

        //Texture dimension value validation
        if(textureDimensionInput.x <= 0 || textureDimensionInput.y <= 0 || textureDimensionInput.x > 2048 || textureDimensionInput.y > 2048)
        {
            return;
        }

        //Get actual mesh and material from the objects
        GameObject mesh = (GameObject)meshInput;
        Material material = (Material)materialInput;

        //Save button
        if(GUILayout.Button("Save Texture"))
        {

            //Generate the texture
            Texture2D generatedTexture = GeneratePositionMap(mesh, material, textureDimensionInput);

            //Texture null check
            if(generatedTexture == null)
            {
                return;
            }

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
    Texture2D GeneratePositionMap(GameObject mesh, Material material, Vector2Int textureDimensions)
    {
        //Find all gameobjects that have a mesh filter and mesh renderer as children of this gameobject
        GameObject[] meshObjects = GetAllObjectsWithMeshAttached(mesh);

        for (int i = 0; i < meshObjects.Length; i++)
        {
            //Debug.Log(meshObjects[i].name);

            //Get the submesh from the main mesh that has this material assigned
            //Mesh objectMesh = IsolateMeshByMaterial(meshObjects[i], material);
        }

        Mesh isolatedMesh = IsolateMeshByMaterial(meshObjects[0], material);

        if(isolatedMesh == null)
        {
            return null;
        }

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

        //Get mesh bounds
        float meshXSize = isolatedMesh.bounds.size.x / 2;
        float meshYSize = isolatedMesh.bounds.size.y / 2;
        float meshZSize = isolatedMesh.bounds.size.z / 2;

        //For every pixel on the x axis
        for (int x = 0; x < textureDimensions.x; x++)
        {
            //For every pixel on the y axis
            for (int y = 0; y < textureDimensions.y; y++)
            {
                //Randomly generate new values
                float R = Random.Range(-meshXSize, meshXSize);
                float G = Random.Range(-meshYSize, meshYSize);
                float B = Random.Range(-meshZSize, meshZSize);

                //Create new colour from values with alpha of 1
                Color pixelColour = new Color(R, G, B, 1.0f);

                //Set the pixel colour
                positionMap.SetPixel(x, y, pixelColour);
            }
        }

        //Apply texture
        positionMap.Apply();

        //Return the texture
        return positionMap;
    }

    //Function to isolate the mesh when given a material
    Mesh IsolateMeshByMaterial(GameObject meshObject, Material material)
    {
        //Get mesh components from prefab
        Mesh mesh = meshObject.GetComponent<MeshFilter>().sharedMesh;
        MeshRenderer renderer = meshObject.GetComponent<MeshRenderer>();

        //This section calculates the submesh that needs to be isolated from the main mesh.
        int submeshNumber = 0;

        bool hasFoundMaterial = false;

        Debug.Log(meshObject.name);

        //For every material in the mesh (every material is a submesh technically)
        for (int i = 0; i < renderer.sharedMaterials.Length; i++)
        {
            Debug.Log(renderer.sharedMaterials[i].name);

            //Is the material at this index in the mesh material array
            if(material.name == renderer.sharedMaterials[i].name)
            {
                //Set submesh number and break out of loop
                submeshNumber = i;
                hasFoundMaterial = true;
                break;
            }
        }

        //Check if the material has been found
        if(hasFoundMaterial == false)
        {
            //If material was not found in the material, if the loop was not broken
            Debug.LogWarning("ParticlePositionMapGenerator: The material was not present on the mesh.");
            return null;
        }

        return MeshExtension.GetSubMesh(mesh, submeshNumber);
    }

    //A function to find all game objects that have a mesh filter attached to them.
    GameObject[] GetAllObjectsWithMeshAttached(GameObject rootObject)
    {
        //Array creation and finding all mesh filters in all children objects
        MeshFilter[] filters = rootObject.GetComponentsInChildren<MeshFilter>();
        //Create game object array with the length of filter components found
        GameObject[] gameObjectsWithMesh = new GameObject[filters.Length];

        //Add all gameobjects that have a filter to the gameobject array
        for (int i = 0; i < filters.Length; i++)
        {
            gameObjectsWithMesh[i] = filters[i].gameObject;
        }

        //Return the gameobject array
        return gameObjectsWithMesh;
    }
}
