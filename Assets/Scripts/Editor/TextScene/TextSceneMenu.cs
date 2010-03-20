/// 
///  Copyright (c) 2010 TerraVision AS
///  See LICENSE file for licensing details
///

using UnityEditor;
using UnityEngine;

/// <summary>
/// All TextScene menu items.
/// </summary>
public class TextSceneMenu
{
	[MenuItem("TextScene/Hierarchy")]
	public static void Hierarchy()
	{
		TextSceneHierarchy.CreateHierarchy();
	}
	
	[MenuItem("TextScene/Load")]
	public static void Load()
	{
		TextSceneDeserializer.Load();
	}
	
	[MenuItem("TextScene/Save as")]
	public static void SaveAs()
	{
		TextSceneSerializer.SaveAs();
	}
	
	[MenuItem("TextScene/Save")]
	public static void SaveCurrent()
	{
		TextSceneSerializer.SaveCurrent();
	}
	
	[MenuItem ("TextScene/Build/Add current to build")]
	public static void AddCurrentToBuild()
	{
		TextScene.AddCurrentSceneToBuild();
	}
	
	[MenuItem ("TextScene/Build/Open Window")]
	public static void OpenWindow()
	{
		TextSceneWindow.Create();
	}
	
	[MenuItem ("Assets/TextScene Open")]
	public static void LoadContext()
	{	
		TextSceneDeserializer.LoadContext();
	}
	
	[MenuItem ("Assets/TextScene add to build")]
	public static void AddContext()
	{
		TextScene.AddSelectedSceneToBuild();
	}
	
	[MenuItem ("TextScene/Warnings/Toggle Default Hierarchy")]
	public static void ToggleDefaultHierarchyWarning()
	{
		int currentValue = PlayerPrefs.GetInt("ShowDefaultHierarchyWarning", 1);
		
		if (currentValue == 0)
		{
			PlayerPrefs.SetInt("ShowDefaultHierarchyWarning", 1);
			EditorUtility.DisplayDialog("Enabled warning", "Default hierarchy warning has been enabled", "OK");
		}
		else
		{
			if (EditorUtility.DisplayDialog("Disable warning", "Are you sure you want to disable the default hierarchy warning? It is recommended to leave this warning on.", "Disable it", "Keep it on"))
				PlayerPrefs.SetInt("ShowDefaultHierarchyWarning", 0);
		}
	}
}

