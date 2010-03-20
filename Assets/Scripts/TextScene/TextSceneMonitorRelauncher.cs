/// 
///  Copyright (c) 2010 TerraVision AS
///  See LICENSE file for licensing details
///

using UnityEngine;
using System.Reflection;

/// <summary>
/// The sole purpose of this component is to re-launch the monitor we use to detect file changes
/// etc in the editor.
/// </summary>
public class TextSceneMonitorRelauncher : MonoBehaviour
{
	void OnDisable()
	{		
		string projectPath = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/'));
		
		string assemblyPath = projectPath + "/Library/ScriptAssemblies/Assembly - CSharp - Editor.dll";
		
		Debug.Log("Loading assembly to launch TextSceneMonitor: " + assemblyPath);
		
		Assembly editorAssembly = Assembly.LoadFile(assemblyPath);
		
		System.Type tsm = editorAssembly.GetType("TextSceneMonitor");
		
		if (tsm != null)
		{
			
			
			MethodInfo mi = tsm.GetMethod("MonitorUpdate", BindingFlags.Static | BindingFlags.Public);
			
			if (mi != null)
			{
				mi.Invoke(null, null);
			}
			else
				Debug.LogError("Unable to find method: MonitorUpdate");
		}
		else
			Debug.LogError("Unable to find type: TextSceneMonitor");
	}
}

