/// 
///  Copyright (c) 2010 TerraVision AS
///  See LICENSE file for licensing details
///
/// TODO:   *Create function to validate all scenes in the build settings and opt the user to create
///         temp scenes if they do not exist (typically fresh checkout where all text scenes are
///         present, but no binary temp version - this will make the player fail in editor mode if
///         the game needs to change level).

using UnityEditor;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using System;


/// <summary>
/// Utility class currently handling custom build settings and a few
/// functions for getting matching text/binary files based on path.
/// </summary>
public static class TextScene
{
	private const string buildSettingsFile = "Library/TextSceneBuildSettings.txt";
	
	/// <summary>
	/// Returns the time the build settings file was last written to.
	/// </summary>
	public static DateTime BuildSettingsDate()
	{
		string fullPath = EditorHelper.GetProjectFolder() + buildSettingsFile;
		
		if (!File.Exists(fullPath))
			return new DateTime(1, 1, 1);
		
		return File.GetLastWriteTime(fullPath);
	}
	
	/// <summary>
	/// Takes a project-relatived temp-scene path and
	/// converts it to the matching TextScene path.
	/// </summary>
	public static string TempToTextSceneFile(string tempScene)
	{
		string textSceneFile = "Assets" + tempScene.Substring(tempScene.IndexOf('/'));
		
		textSceneFile = textSceneFile.Replace(".unity", ".txt");
		
		return textSceneFile;
	}
	
	/// <summary>
	/// Takes a project-relative TextScene filename and
	/// converts it to the matching binary temp file.
	/// </summary>
	public static string TextSceneToTempBinaryFile(string textScene)
	{
		string tempSceneFile = "TempScenes" + textScene.Substring(textScene.IndexOf('/'));
		
		tempSceneFile = tempSceneFile.Replace(".txt", ".unity");
		
		return tempSceneFile;
	}
	
	/// <summary>
	/// Reads in the current list of scenes registered in the custom
	/// TextScene buildsettings.
	/// </summary>
	public static List<string> ReadScenes()
	{
		List<string> sceneList = new List<string>();
		
		string fullPath = EditorHelper.GetProjectFolder() + buildSettingsFile;
		
		if (!File.Exists(fullPath))
			return sceneList;
		
		StreamReader reader = File.OpenText(fullPath);
		
		while(!reader.EndOfStream)
		{
			string line = reader.ReadLine();
			
			string[] elements = line.Trim().Split();
			
			string key = "";
			string val = "";
			
			if (elements.Length >= 2)
			{
				key = elements[0];
				val = elements[1];
			}
			
			if (key == "scene")
				sceneList.Add(val);
		}
		
		reader.Close();
		
		return sceneList;
	}
	
	/// <summary>
	/// Writes a list of project-relative TextScene files to buildsettings. The
	/// corresponding binary unity scenes are stored in the Unity standard
	/// build-settings.
	/// </summary>
	private static void WriteBuildSettings(List<string> sceneList)
	{
		StreamWriter writer = File.CreateText(EditorHelper.GetProjectFolder() + buildSettingsFile);
		
		foreach(string scene in sceneList)
		{
			writer.Write("scene " + scene + '\n');
		}
		
		writer.Close();
	
		//Also update the editor build settings
		List<EditorBuildSettingsScene> binaryScenes = new List<EditorBuildSettingsScene>();
		
		foreach(string scene in sceneList)
		{
			EditorBuildSettingsScene ebss = new EditorBuildSettingsScene();
			
			ebss.path = TextScene.TextSceneToTempBinaryFile(scene);
			ebss.enabled = true;
			
			binaryScenes.Add(ebss);
		}
		
		EditorBuildSettings.scenes = binaryScenes.ToArray();
	}
	
	/// <summary>
	/// Adds a project-relative TextScene to the TextScene buildsettings
	/// and the corresponding binary temp file is written to the standard
	/// Unity build settings.
	/// </summary>
	public static bool AddSceneToBuild(string scene)
	{
		string projectFolder = EditorHelper.GetProjectFolder();
		
		string fullScenePath = projectFolder + scene;
		
		//Make sure the scene file actually exists
		if (!File.Exists(fullScenePath))
		{
			EditorUtility.DisplayDialog("ERROR", "The text scene '" + scene + "' does not seem to exist!", "OK");
			return false;
		}
		
		List<string> sceneList = ReadScenes();
		
		if (sceneList.Contains(scene))
		{
			EditorUtility.DisplayDialog("Already added", "This scene is already in the build list", "OK");
			return false;
		}

		Debug.Log("Added scene to build: " + scene);
		
		sceneList.Add(scene);
		
		WriteBuildSettings(sceneList);
		
		return true;
	}

	/// <summary>
	/// Removes a project-relative TextScene from the TextScene build-settings.
	/// Also updates the standard Unity build settings.
	/// </summary>
	public static void RemoveSceneFromBuild (string scene)
	{
		List<string> sceneList = ReadScenes();
		
		sceneList.Remove(scene);
		
		WriteBuildSettings(sceneList);
	}

	/// <summary>
	/// Moves a scene up or down in the build settings list.
	/// </summary>
	public static void MoveScenePosition (string scene, int direction)
	{
		if (direction == 0)
			return;
		
		List<string> sceneList = ReadScenes();
		
		int index = sceneList.IndexOf(scene);
		
		if (index < 0)
			return;
		
		if (direction > 0)
		{
			if (index+1 >= sceneList.Count)
				return;
			
			string nextEntry = sceneList[index+1];
			
			sceneList[index] = nextEntry;
			sceneList[index+1] = scene;
		}
		else if (direction < 0)
		{
			if (index-1 < 0)
				return;
			
			string prevEntry = sceneList[index-1];
			
			sceneList[index] = prevEntry;
			sceneList[index-1] = scene;
		}
		
		
		WriteBuildSettings(sceneList);
	}
	
	/// <summary>
	/// Adds the currently open scene to the TextScene buildsettings, and updates
	/// the standard Unity buildsettings with the corresponding temp binary scene file.
	/// User will be notified if the current scene is not saved or is not a temp binary
	/// file.
	/// </summary>
	public static void AddCurrentSceneToBuild()
	{
		string currentScene = EditorApplication.currentScene;
		
		if (currentScene.Length == 0)
		{
			EditorUtility.DisplayDialog("Unsaved", "The currently open scene is not saved. Please save it using the TextScene menu option and try again.", "OK");
			return;
		}
		
		if (currentScene.StartsWith("Assets/"))
		{
			EditorUtility.DisplayDialog("Invalid scene", "The currently open scene is not a TextScene. Please re-save using the TextScene menu options and try again", "OK");
			return;
		}
		
		//Debug.Log("Text scene file to save in build settings: " + textSceneFile);
		
		TextScene.AddSceneToBuild(TextScene.TempToTextSceneFile(EditorApplication.currentScene));
	}
	
	/// <summary>
	/// Adds the selected TextScene file to buildsettings. Also updates the standard Unity
	/// buildsettings.
	/// </summary>
	public static void AddSelectedSceneToBuild()
	{
		if (Selection.activeObject == null)
		{
			EditorUtility.DisplayDialog ("Nothing selected", "You need to select a text scene asset to add to build", "OK");	
			return;
		}
		
		TextAsset asset = Selection.activeObject as TextAsset;
		
		string assetPath = "";
		
		if (asset != null)
			assetPath = AssetDatabase.GetAssetPath(asset);
		
		if (!assetPath.EndsWith(".txt"))
			EditorUtility.DisplayDialog ("Not a text file", "Text scenes can be TextAssets (*.txt)", "OK");	
		else
		{
			TextScene.AddSceneToBuild(assetPath);
		}
	}

    /// <summary>
    /// Read and write scenes so unity build settings gets updated.
    /// </summary>
    private static void SyncBuildSettings()
    {
        WriteBuildSettings(ReadScenes());
    }

    //TODO: Finish up and make public!
    public static bool ValidateBuildSettings(out List<string> invalidScenes)
    {
        SyncBuildSettings();

        List<string> sceneList = ReadScenes();

		invalidScenes = new List<string>();

        foreach (string scene in sceneList)
        {
            string absoluteTextScene = EditorHelper.GetProjectFolder() + scene;

            //Make sure the scene exists at all in a TextScene format
            if (!File.Exists(absoluteTextScene))
            {
                Debug.LogWarning("Scene does not exist: " + scene);

                EditorApplication.isPlaying = false;

                if (EditorUtility.DisplayDialog("Invalid scene", "The scene '" + scene + "' is listed in your build settings but it does not exist. Do you want to remove it from build settings?", "Yes", "No"))
                {
                    TextScene.RemoveSceneFromBuild(scene);
                }
                else
				{
                    invalidScenes.Add(scene);
					continue;
				}
            }
            else
            {
                //While the textscene might be present, we also need the binary temp file.
                //Make sure there is one up-to-date, if not, the user should be prompted
                //to generate one.
                string absoluteBinaryTempScene = EditorHelper.GetProjectFolder() + TextSceneToTempBinaryFile(scene);

                if (!File.Exists(absoluteBinaryTempScene))
                {
                    Debug.LogWarning("Temp scene does not exist: " + absoluteBinaryTempScene);

                    //EditorApplication.isPlaying = false;
                    //if (EditorUtility.DisplayDialog("Missing temp file", "Missing temp file for '" + scene + "' - do you want to generate it now?", "Yes", "No"))
                    //    TextSceneDeserializer.LoadSafe(absoluteTextScene);

                    invalidScenes.Add(scene);
					continue;
                }
                else
                {
                    //Both files exist, but we also need to make sure the temp scene isn't outdated.
                    DateTime textSceneTime = File.GetLastWriteTime(absoluteTextScene);
                    DateTime binaryTempSceneTime = File.GetLastWriteTime(absoluteBinaryTempScene);

                    if (textSceneTime > binaryTempSceneTime)
                    {
                        Debug.LogWarning("Temp scene for '" + scene + "' is outdated: " + binaryTempSceneTime + " is older than " + textSceneTime);

                        //EditorApplication.isPlaying = false;
                        //if (EditorUtility.DisplayDialog("Outdated temp file", "Outdated temp file for '" + scene + "' - do you want to update it now?", "Yes", "No"))
                        //    TextSceneDeserializer.LoadSafe(absoluteTextScene);

                        invalidScenes.Add(scene);
						continue;
                    }
                }
            }
        }

        return invalidScenes.Count == 0;
    }
	
	/// <summary>
	/// Builds a self-launching player based on the passed BuildTarget parameter. Will go through all
	/// scenes in the buildsettings list and make sure they have a temporary binary file to build. Will
	/// notify user if anything went wrong (such as empty build settings or unsupported build target).
	/// </summary>
	public static void Build(BuildTarget buildTarget)
    {
		string extension = "";
		
		if (buildTarget == BuildTarget.WebPlayer
		    || buildTarget == BuildTarget.WebPlayerStreamed)
			extension = "unity3d";
		else if (buildTarget == BuildTarget.StandaloneWindows)
			extension = "exe";
		else if (buildTarget == BuildTarget.StandaloneOSXUniversal)
			extension = "app";
		
		if (extension.Length == 0)
		{
			EditorUtility.DisplayDialog("Build target not supported", "Build target not currently supported: " + buildTarget.ToString(), "OK");
			return;
		}

        string startPath = Application.dataPath;

        startPath = startPath.Substring(0, startPath.LastIndexOf('/')) + "/Build";

        if (!Directory.Exists(startPath))
            Directory.CreateDirectory(startPath);

        string path = EditorUtility.SaveFilePanel("Build target", startPath, "build", extension);

        if (path.Length == 0)
            return;
		
		new TextSceneBuilder(path, buildTarget);
	}
	
	public static void BuildTempScenes(List<string> scenes)
	{
		new TextSceneBuilder(null, BuildTarget.WebPlayer, scenes);
	}
	
	public static void BuildTempScenes()
	{
		//TODO: Get rid of unused parameters in constructor.
		new TextSceneBuilder(null, BuildTarget.WebPlayer);
	}
}

class TextSceneBuilder
{
	private List<string> binarySceneList = new List<string>();
	private Queue<string> sceneQueue;
	private string sceneToLoad;
	private string buildPath;
	private BuildTarget buildTarget;
	
	public TextSceneBuilder(string buildPath, BuildTarget buildTarget) : this(buildPath, buildTarget, null)
	{
		
	}
	
	public TextSceneBuilder(string buildPath, BuildTarget buildTarget, List<string> sceneList)
	{
		if (sceneList == null)
			sceneList = TextScene.ReadScenes();

		if (sceneList.Count == 0)
		{
			EditorUtility.DisplayDialog("No scenes", "No scenes have been added to the build settings. Please add some, and try to build again.", "OK");
			return;
		}
		
		this.sceneQueue = new Queue<string>(sceneList);
		this.sceneToLoad = TextSceneMonitor.Instance.GetCurrentScene();
		this.buildPath = buildPath;
		this.buildTarget = buildTarget;

        if (EditorApplication.SaveCurrentSceneIfUserWantsTo())
        {
            TextSceneMonitor.Instance.SaveIfTempIsNewer();
        }
        else
        {
            Debug.Log("Cancelled build");
            return;
        }
		
		BuildNext();	
	}

	private void BuildNext()
	{
		//TODO: Must be fixed up to work with callback - scene saving does not work very well if done
		//      immediately after load, which is why we need to make this into a async operation
		//      of some sort. See unity case 336621.
        //Run through all scenes and build them based on the human readable representation.
		if (sceneQueue.Count > 0)
		{
			string textScene = sceneQueue.Dequeue();
			
			Debug.Log("Building temp for: " + textScene);
			
            string result = TextSceneDeserializer.Load(EditorHelper.GetProjectFolder() + textScene, this.BuildNext);

            if (result.Length == 0)
            {
                EditorUtility.DisplayDialog("Scene does not exist", "Unable to find scene file: " + textScene + " Will be excluded from build", "OK");
                BuildNext();
            }
            else
			    binarySceneList.Add(result);
		}
		else
			FinishBuild();
	}
	
	private void FinishBuild()
	{
		Debug.Log("Building " + binarySceneList.Count + " scenes: ");
		
		foreach(string scene in binarySceneList)
		{
			Debug.Log(scene);
		}
		
		if (buildPath != null)
		{
			BuildPipeline.BuildPlayer(binarySceneList.ToArray(), buildPath, buildTarget, BuildOptions.AutoRunPlayer);
		}
		else
		{
			Debug.Log("Temp scenes generated, no player will be built.");
		}
		
		if (sceneToLoad.Length > 0)
			TextSceneDeserializer.Load(sceneToLoad);
	}
}
