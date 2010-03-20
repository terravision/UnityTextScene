/// 
///  Copyright (c) 2010 TerraVision AS
///  See LICENSE file for licensing details
///

using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Timers;

/// <summary>
/// Class monitoring the state of TextScenes and user action. Tries to identify situations where
/// the user does something he/she didn't want to, such as saving using built-in save functionality,
/// using regular binary unity scenes together with TextScenes etc. Also checks for externally changed
/// files, which is handy when using VCS.
/// 
/// FIXME: This class could have been a lot cleaner if there were hooks for the most common user actions
///        and editor events.
/// </summary>
[Serializable]
public class TextSceneMonitor
{
	private static TextSceneMonitor instance;
	//private static System.Timers.Timer timer;
	
	public static TextSceneMonitor Instance
	{
		get
		{
			if (instance == null)
			{
				instance = new TextSceneMonitor();
				/*
				timer = new System.Timers.Timer(1000);
				
				timer.Elapsed += MonitorUpdate;
				timer.AutoReset = true;
				timer.Enabled = true;
				*/
				//FIXME: These seem to get lost during play sessions?
				EditorApplication.update += MonitorUpdate;
				EditorApplication.playmodeStateChanged += MonitorStateChange;
				
				
			}
		
			return instance;
		}
	}

    private static void MonitorStateChange()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (!TextScene.ValidateBuildSettings())
            {
                EditorApplication.isPlaying = false;

                if (EditorUtility.DisplayDialog("Build settings", "Errors were found while validating Build Settings. Your levels may not switch correctly in play-mode! Do you want to fix up any outdated/missing temp file issues now?", "Yes", "No"))
                    TextScene.BuildTempScenes();
            }
        }
    }

	/*
	private static void MonitorStateChange()
	{                                       
		if (EditorApplication.isPlayingOrWillChangePlaymode)
		{
			Debug.Log("Editor about to start playmode: " + EditorApplication.timeSinceStartup.ToString("F3"));
			
			//HACK: We're creating this so it can tell us whenever we leave playmode :S
			GameObject go = new GameObject("TextSceneMonitorRelauncher");
            go.hideFlags = HideFlags.NotEditable;
			go.AddComponent<TextSceneMonitorRelauncher>();
		}
		else
		{
			Debug.Log("Editor not in playmode: " + EditorApplication.timeSinceStartup.ToString("F3"));
			
			TextSceneMonitorRelauncher[] objects  = Helper.GetObjectsOfType<TextSceneMonitorRelauncher>();
		
			foreach(TextSceneMonitorRelauncher o in objects)
				GameObject.DestroyImmediate(o.gameObject);
		}
	}
	*/
/*	
	public static void MonitorCallbacks(object sender, System.Timers.ElapsedEventArgs e)
	{
		Debug.Log("Setting editor callbacks");
		
		//EditorApplication.update += MonitorUpdate;
		EditorApplication.playmodeStateChanged += MonitorStateChange;
	}
*/	
	
	/// <summary>
	/// Wrapper for running an update on the monitor.
	/// </summary>
	public static void MonitorUpdate()
	{	
		Instance.Update();
	}
	
	private double nextCheckForChangedFile = 0.0f;
	
	//Absolute path of currently monitored scene, and when it was last loaded.
	private string currentScene = "";
    private string currentTempBinaryScene = "";
	private DateTime currentSceneLoaded;
	
	//Current scene open. If this changes unexpectedly, the user will be notified.
	private string alarmingEditorScene;
	
    //HACK: To get around bug with save/load instantly after instantiating prefabs.
	private int saveAndReloadTimer = 0;
	private string saveAndReload = "";
	private TextSceneDeserializer.TempSceneSaved saveAndReloadCallback = null;
	
	/// <summary>
	/// Creates a new TextSceneMonitor and reads in relevant values from PlayerPrefs (such as
	/// last monitored scene).
	/// </summary>
	public TextSceneMonitor()
	{
		currentScene = PlayerPrefs.GetString("TextSceneMonitorCurrentScene", "");

		alarmingEditorScene = PlayerPrefs.GetString("TextSceneAlarmingEditorScene", "");

        currentTempBinaryScene = EditorHelper.GetProjectFolder() + alarmingEditorScene;

        //Use file timestamp instead of DateTime.Now for consistency reasons.
        if (File.Exists(currentTempBinaryScene))
        {
            currentSceneLoaded = File.GetLastWriteTime(currentTempBinaryScene);
        }
        else
        {
            Debug.LogWarning("Unable to find temp file: " + currentTempBinaryScene);
            currentSceneLoaded = DateTime.Now;
        }

		Debug.Log("Creating new TextSceneMonitor instance: " + currentScene);
	}
	                    
	/// <summary>
	/// Sets the current TextScene we will monitor for changes. Also stores the current
	/// scene Unity has open (the binary version) so we can detect if the user suddenly
	/// changes to either a new scene or a different binary scene without going via
	/// the TextScene functionality.
	/// </summary>
	/// <param name="filename">
	/// A <see cref="System.String"/> holding the full absolute path to the TextScene file.
	/// </param>
	public void SetCurrentScene(string filename)
	{
		alarmingEditorScene = EditorApplication.currentScene;
        currentTempBinaryScene = EditorHelper.GetProjectFolder() + alarmingEditorScene;
		
		currentScene = filename;

        //Use file timestamp instead of DateTime.Now for consistency reasons.
        if (File.Exists(currentTempBinaryScene))
        {
            currentSceneLoaded = File.GetLastWriteTime(currentTempBinaryScene);
        }
        else
        {
            Debug.LogWarning("Unable to find temp file: " + currentTempBinaryScene);
            currentSceneLoaded = DateTime.Now;
        }

        Debug.Log("TextSceneMonitor setting current scene: " + filename + " (" + currentSceneLoaded + ")");
		
		nextCheckForChangedFile = EditorApplication.timeSinceStartup + 1.0f;
		
		PlayerPrefs.SetString("TextSceneMonitorCurrentScene", currentScene);
		PlayerPrefs.SetString("TextSceneAlarmingEditorScene", alarmingEditorScene);
	}
	
	/// <summary>
	/// Returns the current scene being monitored.
	/// </summary>
	/// <returns>
	/// A <see cref="System.String"/> holding the absolute path to the currently monitored scene.
	/// </returns>
	public string GetCurrentScene()
	{
		return currentScene;
	}
	
	private const float SaveAndReloadFrames = 5.0f;
	
	public void DoSaveAndReload(string scene, TextSceneDeserializer.TempSceneSaved callback)
	{
		//Must wait a couple of frames before we do the save for prefabs to behave correctly.
		//TODO: Report bug.
		saveAndReloadTimer = Mathf.RoundToInt(SaveAndReloadFrames);
		saveAndReload = scene;
		saveAndReloadCallback = callback;
	}
	
	/// <summary>
	/// Regularly checks if something worthy of notifying the user happens. This includes
	/// unexpected scene changes (double-clicking .unity files), creating new scenes and
	/// source TextScene files having been changed between now and the time it was loaded.
	/// </summary>
	private void Update()
	{
		//HACK: To get around bug (TOOD: Insert case #) where Save immediately
		//      after instantiating prefabs results in weird behaviour.
		if (saveAndReloadTimer > 0)
		{
			saveAndReloadTimer--;
		
			EditorUtility.DisplayProgressBar("Saving...", "Saving binary temp file for TextScene", 1.0f - saveAndReloadTimer/SaveAndReloadFrames);
			
			
			if (saveAndReload.Length > 0 && saveAndReloadTimer <= 0)
			{
				EditorApplication.SaveScene(saveAndReload);
				EditorApplication.OpenScene(saveAndReload);

                string writtenFile = EditorHelper.GetProjectFolder() + EditorApplication.currentScene;

                DateTime writtenTime = File.GetLastWriteTime(writtenFile);

                Debug.Log("Wrote temp file at " + writtenTime);
			
				TextSceneMonitor.Instance.SetCurrentScene(EditorHelper.GetProjectFolder() + TextScene.TempToTextSceneFile(EditorApplication.currentScene));
				
				saveAndReload = "";
				
				EditorUtility.ClearProgressBar();
				
				TextSceneDeserializer.TempSceneSaved callbackCopy = saveAndReloadCallback;
				saveAndReloadCallback = null;
				
				if (callbackCopy != null)
					callbackCopy();
			}
			
			return;
		}
		
        //Did the user create a new scene?
		if (currentScene.Length > 0
		    && EditorApplication.currentScene.Length == 0)
		{
			if (CheckForChangedTemp())
				EditorApplication.NewScene();
			
			currentScene = "";
			alarmingEditorScene = "";
			
			TextSceneSerializer.SaveCurrent();
			
			//Warn the user if the save scene dialog was cancelled.
			if (currentScene.Length == 0)
				EditorUtility.DisplayDialog("New Scene", "You have started a new scene after using the TextScene system. Please use the TextScene menu to save it if you want to continue using the TextScene system", "OK");
		}
		
		//Try to detect when we go from a TextScene to a regular unit scene.
		if ((currentScene.Length > 0
		    && EditorApplication.currentScene.Length > 0)
		    || (currentScene.Length == 0
		    && alarmingEditorScene.Length == 0))
		{
			if (alarmingEditorScene != EditorApplication.currentScene)
			{
				string current = EditorApplication.currentScene;
				
				if (CheckForChangedTemp())
					EditorApplication.OpenScene(current);
				
				if (EditorUtility.DisplayDialog("TextScene/built-in mixed usage", "It is not recommended to mix TextScene usage with built-in Unity scenes. This may cause the TextScene system to miss updates or simply behave totally weird! If you plan on using the TextScene system, you should save the current scene via the TextScene menu item and  - if successfully saved (please inspect the log for errors and warnings) - remove the original from the Assets folder. Please note that not all components/Unity objects can be saved into the TextScene format, so don't delete the original until you are 100% sure you saved what you need!", "Save to TextScene now!", "I know what I'm doing"))
				{
					TextSceneSerializer.SaveCurrent();
				}
				else
				{
					alarmingEditorScene = EditorApplication.currentScene;
					currentScene = "";
				}
			}
		}
		
		//Regular checks to see if the scene file was edited since our last load.
		if (currentScene.Length > 0
		    && EditorApplication.timeSinceStartup > nextCheckForChangedFile)
		{
			if (File.Exists(currentScene))
			{
                DateTime lastWriteTime = File.GetLastWriteTime(currentScene);

                if (lastWriteTime > currentSceneLoaded)
				{
                    int result = EditorUtility.DisplayDialogComplex("Scene changed", "The TextScene you currently have open changed (" + lastWriteTime + "). Do you want to reload it?", "Yes", "Backup mine first", "No");
					
					if (result == 0)//Yes
					{
						TextSceneDeserializer.Load(currentScene);
					}
                    else if (result == 1)//Backup first
                    {
                        string filename = EditorUtility.SaveFilePanel("Backup TextScene", currentScene.Substring(0, currentScene.LastIndexOf('/')), "backup", "txt");

                        if (filename.Length != 0)
                        {
                            //This is overwritten during the save.
                            string toLoad = currentScene;

                            TextSceneSerializer.Save(filename);
                            TextSceneDeserializer.Load(toLoad);
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Unsaved", "You chose to cancel the backup of your own scene file. It is recommended that you manually save a copy, and merge with the updated file from disk (" + currentScene + ")", "OK");
                        }
                    }
                    else//No
                    {
                        //HACK: Shut this message up. We don't want to get asked this again until the
                        //      file changes again.
                        currentSceneLoaded = DateTime.Now;
                    }
				}
			}
			else
			{
				if (EditorUtility.DisplayDialog("TextScene file gone!", "It seems like the TextScene representation of your open file has been deleted. Do you want to re-save it (" + currentScene + ")?", "Yes", "No"))
					TextSceneSerializer.Save(currentScene);
				else
					currentScene = "";
			}
			
            //Also check for changed temp files (.unity in TempScenes). This happens if
            //the user uses the built-in save functionality.
            CheckForChangedTemp();
				    
			nextCheckForChangedFile = EditorApplication.timeSinceStartup + 1.0f;		    
		}
	}

    public void SaveIfTempIsNewer()
    {
        if (File.Exists(currentTempBinaryScene))
        {
            if (File.GetLastWriteTime(currentTempBinaryScene) > currentSceneLoaded)
            {
                TextSceneSerializer.SaveCurrent();
                Debug.Log("Temp file updated: " + currentTempBinaryScene);
            }
            else
                Debug.Log("Temp file already up to date: " + currentTempBinaryScene);
        }
        else
            Debug.LogWarning("Temp file does not exist!");
    }
	
	private bool CheckForChangedTemp()
	{
		//Also check for temp binary file changes, so we can give the user some options.
        if (File.Exists(currentTempBinaryScene))
        {
            DateTime lastTempWrite = File.GetLastWriteTime(currentTempBinaryScene);

            if (lastTempWrite > currentSceneLoaded)
            {
                if (EditorUtility.DisplayDialog("Temp scene changed", "The temp scene changed (" + lastTempWrite + "), this probably means that either you or Unity saved using the standard menu items - you should re-save using the TextScene menu if you want these changes to be kept!", "Re-save now", "I'll do it later"))
				{
					if (EditorApplication.currentScene != alarmingEditorScene)
						EditorApplication.OpenScene(alarmingEditorScene);
					
					
					TextSceneSerializer.SaveCurrent();
					return true;
				}
                else
                    SetCurrentScene(currentScene);//Reset the timers so we don't get this popping up more than once per save.
                
            }
        }
		
		return false;
	}
}