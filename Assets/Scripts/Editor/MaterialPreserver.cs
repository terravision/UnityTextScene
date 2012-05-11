/// 
///  Copyright (c) 2010 TerraVision AS
///  See LICENSE file for licensing details
///

using UnityEngine;
using UnityEditor;
using System.IO;

public class MaterialPreserver : AssetPostprocessor
{
    public Material OnAssignMaterialModel(Material material, Renderer renderer)
    {
        string subfolderName = assetPath.Substring(0, assetPath.LastIndexOf("/")) + "/Materials";
        string materialFileName = "/" + material.name + ".mat";

        if (material.name == null || material.name.Length == 0)
        {
			subfolderName = "Assets/Materials";
			materialFileName = "/Unnamed.mat";
        }

        string assetMaterialDirectory = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("/")) + "/" + subfolderName;

		//Debug.Log(assetMaterialDirectory);
		
        if (!Directory.Exists(assetMaterialDirectory))
        {
            Debug.Log("Creating directory for material (" + material.name + "): " + assetMaterialDirectory);
            Directory.CreateDirectory(assetMaterialDirectory);
        }


        string materialAssetPath = subfolderName + materialFileName;

        //Use the existing material if there is one. If you want to regenerate the material,
        //delete the file itself.
        Material existing = AssetDatabase.LoadAssetAtPath(materialAssetPath, typeof(Material)) as Material;

        if (existing != null)
        {
            Debug.Log("Material (" + material.name + ") already exists, using that one");
            return existing;
        }


        

        Debug.Log("Material (" + material.name + ") does not exist, creating new one at " + materialAssetPath);

        
        // Create a new material asset using the specular shader
        // but otherwise the default values from the model
        material.shader = Shader.Find("Specular");
        AssetDatabase.CreateAsset(material, materialAssetPath);
        return material;
    }
}