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
        List<float> meshVolumeList = new List<float>();
        float[] meshVolumes;
        float totalMeshVolume = 0;

        //For every object with the mesh attached to it
        for (int i = 0; i < meshObjects.Length; i++)
        {
            //Get the submesh from the main mesh that has this material assigned
            Mesh objectMesh = IsolateMeshByMaterial(meshObjects[i], material);
            
            //Null check
            if(objectMesh == null)
            {
                continue;
            }

            //Calculate the volume of the mesh and add to list and add to total volume
            float volume = MeshExtension.VolumeOfMesh(objectMesh);
            meshVolumeList.Add(volume);
            totalMeshVolume += volume;
        }

        meshVolumes = meshVolumeList.ToArray();

        //If the mesh volumes length is 0, if no meshes had the material found
        if(meshVolumes.Length == 0)
        {
            Debug.LogError("The object that was entered did not have the matching material anywhere in it's hierarchy.");
            return null;
        }

        //Calculating the number of pixels that should be used for each mesh
        int[] numberOfPixelsPerMesh = new int[meshVolumes.Length];
        int textureSize = textureDimensions.x * textureDimensions.y;
        int addedPixelCount = 0;

        //For every mesh volume
        for (int i = 0; i < meshVolumes.Length; i++)
        {
            //If on last index, this is important for making sure the remainder of percentage is filled out
            if(i == meshVolumes.Length - 1)
            {
                //Calculate the number of pixels remaining
                numberOfPixelsPerMesh[i] = textureSize - addedPixelCount;
            }
            else
            {
                //Calculate the multiplier for the pixel count for this mesh
                float multiplierForPixelCount = meshVolumes[i] / totalMeshVolume;
                //Calculate the number of pixels that should be assigned to this mesh
                numberOfPixelsPerMesh[i] = Mathf.FloorToInt(multiplierForPixelCount * textureSize);
                //Add pixel count on for calculating final mesh pixel count
                addedPixelCount += numberOfPixelsPerMesh[i];
            }
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
        List<int> submeshNumbers = new List<int>();

        bool hasFoundMaterial = false;
        //For every material in the mesh (every material is a submesh technically)
        for (int i = 0; i < renderer.sharedMaterials.Length; i++)
        {
            //Is the material at this index in the mesh material array
            if(material.name == renderer.sharedMaterials[i].name)
            {
                //Add submesh number to list and mark flag as true so the function does not exit
                submeshNumbers.Add(i);
                hasFoundMaterial = true;
            }
        }

        //Check if the material has been found
        if(hasFoundMaterial == false)
        {
            //If material was not found in the mesh, then return out
            return null;
        }

        //Get the submeshes
        CombineInstance[] combine = new CombineInstance[submeshNumbers.Count];

        //Set combine submesh data
        for (int i = 0; i < submeshNumbers.Count; i++)
        {
            combine[i].mesh = MeshExtension.GetSubMesh(mesh, submeshNumbers[i]);
            combine[i].transform = meshObject.transform.localToWorldMatrix;
        }

        //Combine the meshes
        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combine);

        //Return the combined mesh
        return combinedMesh;
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
