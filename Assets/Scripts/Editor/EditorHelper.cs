/// 
///  Copyright (c) 2010 TerraVision AS
///  See LICENSE file for licensing details
///

using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;


public class EditorHelper
{
	public static void ClearLog()
	{
	    Assembly assembly = Assembly.GetAssembly(typeof(SceneView));
	
	    Type type = assembly.GetType("UnityEditor.LogEntries");
	    MethodInfo method = type.GetMethod("Clear");
	    method.Invoke(new object(), null);
	}
	
	public static string GetProjectFolder()
	{
		string dataPath = Application.dataPath;
		
		return dataPath.Substring(0, dataPath.LastIndexOf('/')+1);
	}

    public static System.Object[] ParamList(params System.Object[] paramList)
    {
        return paramList;
    }

    private static MethodInfo textContentMethod;

    public static GUIContent TextContent(string text)
    {
        if (textContentMethod == null)
            textContentMethod = typeof(EditorGUIUtility).GetMethod("TextContent", BindingFlags.Static | BindingFlags.NonPublic);

        return textContentMethod.Invoke(null, EditorHelper.ParamList(text)) as GUIContent;
    }
}

