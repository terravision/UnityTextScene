/// 
///  Copyright (c) 2010 TerraVision AS
///  See LICENSE file for licensing details
///

using UnityEngine;
using UnityEditor;

public class PlayerPrefsTools
{
    [MenuItem("Tools/PlayerPrefsTools/Delete All")]
    public static void DeleteAll()
    {
        if (EditorUtility.DisplayDialog("Warning", "This will clear the playerprefs! Are you sure?", "Yes", "No"))
            PlayerPrefs.DeleteAll();
    }
}