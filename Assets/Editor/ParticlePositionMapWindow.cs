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
        EditorGUILayout.LabelField("Texture Dimensions (MAX: 1024 X 1024)");
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
        if(textureDimensionInput.x <= 0 || textureDimensionInput.y <= 0 || textureDimensionInput.x > 1024 || textureDimensionInput.y > 1024)
        {
            return;
        }

        //Get actual mesh and material from the objects
        GameObject mesh = (GameObject)meshInput;
        Material material = (Material)materialInput;

        //Save button
        if(GUILayout.Button("Save Position Map"))
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
                    Debug.Log("Successfully created position map and stored it at: " + assetPath);
                }
            }
        }
    }

    //Function to generate the position map.
    Texture2D GeneratePositionMap(GameObject mesh, Material material, Vector2Int textureDimensions)
    {
        //Find all gameobjects that have a mesh filter and mesh renderer as children of this gameobject
        GameObject[] meshObjects = GetAllObjectsWithMeshAttached(mesh);
        List<GameObject> validMeshes = new List<GameObject>();

        //For every object with the mesh attached to it
        for (int i = 0; i < meshObjects.Length; i++)
        {
            //Get the submesh from the main mesh that has this material assigned
            bool isValidMesh = HasMeshGotMaterial(meshObjects[i], material);
            
            //Check if is valid mesh
            if(isValidMesh == false)
            {
                //If it is not, then skip this object
                continue;
            }

            //If mesh had the material, add to valid meshes array
            validMeshes.Add(meshObjects[i]);
        }
        
        //If the valid mesh length is 0, if no meshes had the material found
        if(validMeshes.Count == 0)
        {
            Debug.LogError("The object that was entered did not have the matching material anywhere in it's hierarchy.");
            return null;
        }

        //Combining submeshes into mesh for calculating surface area of triangles and particle mapping
        Mesh combinedMesh = new Mesh();
        CombineInstance[] combineInstances = new CombineInstance[validMeshes.Count];

        //For every valid mesh / combine instance
        for (int i = 0; i < combineInstances.Length; i++)
        {
            //Set combine instance data
            combineInstances[i].mesh = IsolateMeshByMaterial(validMeshes[i], material);
            combineInstances[i].transform = validMeshes[0].transform.localToWorldMatrix;
        }
        //Combine the meshes
        combinedMesh.CombineMeshes(combineInstances);

        //Null check
        if (combinedMesh == null)
        {
            return null;
        }

        //Calculating surface area
        float totalMeshSurfaceArea = MeshExtension.SurfaceAreaOfMesh(combinedMesh);
        float[] triangleSurfaceAreas = MeshExtension.SurfaceAreaOfMeshTriangles(combinedMesh);

        //Calculating the number of pixels to dedicate the each triangle on the position map to ensure an even distribution
        int[] numberOfPixelsPerTriangle = new int[triangleSurfaceAreas.Length];
        int textureSize = textureDimensions.x * textureDimensions.y;
        int addedPixelCount = 0;

        //For every triangle surface area
        for (int i = 0; i < triangleSurfaceAreas.Length; i++)
        {
            //If on last index, this is important for making sure the remainder of percentage is filled out
            if (i == triangleSurfaceAreas.Length - 1)
            {
                //Calculate the number of pixels remaining
                numberOfPixelsPerTriangle[i] = textureSize - addedPixelCount;
            }
            else
            {
                //Calculate the multiplier for the pixel count for this triangle
                float multiplierForPixelCount = triangleSurfaceAreas[i] / totalMeshSurfaceArea;
                //Calculate the number of pixels that should be assigned to this triangle
                numberOfPixelsPerTriangle[i] = Mathf.FloorToInt(multiplierForPixelCount * textureSize);
                //Add pixel count on for calculating final triangle pixel count
            }
            addedPixelCount += numberOfPixelsPerTriangle[i];
        }

        //Generating position data
        Vector3[] positions = MeshExtension.RandomPointsWithinMesh(combinedMesh, numberOfPixelsPerTriangle);

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

        //Binding position data to the texture
        int positionCounter = 0;

        //For every pixel on the x axis
        for (int x = 0; x < textureDimensions.x; x++)
        {
            //For every pixel on the y axis
            for (int y = 0; y < textureDimensions.y; y++)
            {
                Vector3 pos;

                //Safety catch in order to prevent crash of program
                if(positionCounter >= positions.Length)
                {
                    pos = new Vector3();
                    Debug.LogError("Position Mapping Error: Position array was not long enough for number of pixels in texture dimensions.");
                }
                else
                {
                    //Get the position at the correct index
                    pos = positions[positionCounter];

                }


                //Create new colour from values with alpha of 1
                Color pixelColour = new Color(pos.x, pos.y, pos.z, 1.0f);

                //Set the pixel colour
                positionMap.SetPixel(x, y, pixelColour);
                positionCounter++;
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

    //Function to test if a mesh has a material
    bool HasMeshGotMaterial(GameObject meshObject, Material material)
    {
        MeshRenderer renderer = meshObject.GetComponent<MeshRenderer>();

        //For every material in the mesh (every material is a submesh technically)
        for (int i = 0; i < renderer.sharedMaterials.Length; i++)
        {
            //Is the material at this index in the mesh material array
            if (material.name == renderer.sharedMaterials[i].name)
            {
                //Material has been found, return out
                return true;
            }
        }
        //Material was not found in loop, return out.
        return false;
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
