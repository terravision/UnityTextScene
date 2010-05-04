/// 
///  Copyright (c) 2010 TerraVision AS
///  See LICENSE file for licensing details
///
///  TODO:  *Make this look less crap.
///         *Rename to something better, probably including "BuildSettings".
///         *Check for built-in build settings window and alert user if it is up
///         (similar to TextSceneHierarchy whining if the built-in is up).

using UnityEngine;
using UnityEditor;

using System.IO;
using System.Collections.Generic;
using System;
using System.Text;


/// <summary>
/// Editor window currently only holding the build settings and build options. 
/// </summary>
public class TextSceneWindow : EditorWindow
{
    public static void Create () 
	{
        // Get existing open window or if none, make a new one:
        TextSceneWindow window = (TextSceneWindow)EditorWindow.GetWindow (typeof (TextSceneWindow));
        
		window.title = "TextScene Build Settings";
		window.Show ();
		window.LoadSettings();
    }
	
	double nextBuildSettingsCheck = 0.0;
	DateTime loadedBuildSettingsTime;
	
	List<string> scenes = new List<string>();

    Vector2 scroll = Vector2.zero;
	
	void LoadSettings()
	{
		scenes = TextScene.ReadScenes();
		
		loadedBuildSettingsTime = TextScene.BuildSettingsDate();
	}
    
    void OnGUI () 
	{
		GUILayout.BeginVertical();

        scroll = GUILayout.BeginScrollView(scroll);

        GUILayout.Label("Scenes to build");
		
		foreach(string scene in scenes)
		{
			EditorGUILayout.BeginHorizontal();
			
			if (GUILayout.Button(scene))
			{
                TextSceneDeserializer.LoadSafe(EditorHelper.GetProjectFolder() + scene);
                GUIUtility.ExitGUI();
                return;
			}
			
			
			if (GUILayout.Button("Remove", GUILayout.MaxWidth(60)))
			{	
				if (EditorUtility.DisplayDialog("Remove scene", "Are you sure you want to remove this scene from the build settings?", "Yes", "No"))
				{
					TextScene.RemoveSceneFromBuild(scene);
					LoadSettings();
				}
			}

			if (GUILayout.Button("+", GUILayout.MaxWidth(20)))
			{
				TextScene.MoveScenePosition(scene, 1);
				LoadSettings();
			}
			
			if (GUILayout.Button("-", GUILayout.MaxWidth(20)))
			{
				TextScene.MoveScenePosition(scene, -1);
				LoadSettings();
			}
			
			GUILayout.EndHorizontal();
		}
		
		if (scenes.Count > 0)
		{
            EditorGUILayout.Separator();
			EditorGUILayout.BeginVertical(GUILayout.MaxWidth(150));
			
			if (GUILayout.Button("Build Temp"))
			{
				TextScene.BuildTempScenes();
				GUIUtility.ExitGUI();
				return;
			}

            if (GUILayout.Button("Build Streamed Web"))
            {
                TextScene.Build(BuildTarget.WebPlayerStreamed);
                GUIUtility.ExitGUI();
                return;
            }

            if (GUILayout.Button("Build Web"))
            {
                TextScene.Build(BuildTarget.WebPlayer);
                GUIUtility.ExitGUI();
                return;
            }
			
			if (GUILayout.Button("Build OSX"))
            {
                TextScene.Build(BuildTarget.StandaloneOSXUniversal);
                GUIUtility.ExitGUI();
                return;
            }
			
			if (GUILayout.Button("Build Windows"))
            {
                TextScene.Build(BuildTarget.StandaloneWindows);
                GUIUtility.ExitGUI();
                return;
            }
			
			EditorGUILayout.EndVertical();
		}
		else
		{
			GUILayout.Label("Add scenes via the TextScene menu item to enable build");
		}
		
		EditorGUILayout.Separator();
		
		if (GUILayout.Button("Add current", GUILayout.MaxWidth(100)))
		{
			TextScene.AddCurrentSceneToBuild();
			LoadSettings();
		}

        EditorGUILayout.Separator();

        if (GUILayout.Button("Validate Settings", GUILayout.MaxWidth(100)))
        {
			List<string> invalidScenes;
			
            if (TextScene.ValidateBuildSettings(out invalidScenes))
                EditorUtility.DisplayDialog("Valid settings", "The build settings seem valid enough", "OK");
            else
			{
				StringBuilder sb = new StringBuilder();
				
				sb.Append("There were errors in validation: \n");
				
				foreach(string scene in invalidScenes)	
				{
					sb.Append("   ");
					sb.Append(scene);
					sb.Append('\n');
				}
				
				sb.Append("Try running 'Build Temp' to fix them up, or inspect the console for further hints.");
				
                EditorUtility.DisplayDialog("Validation failed", sb.ToString(), "OK");
			}
        }

        GUILayout.EndScrollView();

        GUILayout.EndVertical();
    }
	
	void OnHierarchyChange()
	{
		//TODO/FIXME: Detect this and set TextSceneMonitor to dirty so we can warn users if they try to
		//            load a different scene.

        //UPDATE: Tried to go around this by using 'SaveIfUserWantsTo' where possible.
	}
	
	void OnInspectorUpdate()
	{
		//TODO FIXME HACK: I can't currently see how to make the instance survive a play/stop session,
		//                 so we just make sure it is constantly requested (and renewed if necessary).
		//UPDATE: Moved this to a possibly even worse hack, but at least it works without needing to have
		//        this editor window active.
		//TextSceneMonitor.MonitorUpdate();
		
		
		if (EditorApplication.timeSinceStartup > nextBuildSettingsCheck)
		{
			nextBuildSettingsCheck = EditorApplication.timeSinceStartup + 2.0f;
			
			DateTime buildSettingsTime = TextScene.BuildSettingsDate();
			
			if (buildSettingsTime > loadedBuildSettingsTime)
			{
				LoadSettings();
				loadedBuildSettingsTime = buildSettingsTime;
			}
		}
	}
}