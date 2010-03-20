/// 
///  Copyright (c) 2010 TerraVision AS
///  See LICENSE file for licensing details
///

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TextSceneObject))]
public class TextSceneInspector : Editor
{
	public override void OnInspectorGUI()
	{
		TextSceneObject tso = target as TextSceneObject;
		
		EditorGUILayout.BeginVertical();
		
		GUILayout.Label("External scene");
		
		EditorGUILayout.BeginHorizontal ();
        GUILayout.Label("Open scene: ");
		
		if (GUILayout.Button(tso.textScene.name))
		{
            if (EditorUtility.DisplayDialog("Open scenefile?", "Do you want to close the current scene and open the scene pointed to by this TextSceneObject?", "Yes", "No"))
            {
                TextSceneDeserializer.LoadSafe(EditorHelper.GetProjectFolder() + AssetDatabase.GetAssetPath(tso.textScene));
                GUIUtility.ExitGUI();
            }
		}
		
        EditorGUILayout.EndHorizontal ();
		EditorGUILayout.EndVertical();
	}
}

