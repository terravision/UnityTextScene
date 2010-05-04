/// 
///  Copyright (c) 2010 TerraVision AS
///  See LICENSE file for licensing details
///
///  TODO:  *Don't write component/field count at the start of each listing - makes editing more complicated
///         than it needs to be. This must of course be reflected in TextSceneDeserializer.
///         *Prefabs with changed instance parameters are not supported (it will be 'reverted' on scene load).

using UnityEngine;
using UnityEditor;

using System.Xml.Serialization;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Class responsible for writing Unity scene hierarchies to a textual, humanly readable format. 
/// </summary>
public class TextSceneSerializer
{
	private static int warnings = 0;
	
    public static void SaveAs()
    {
        string currentScene = EditorApplication.currentScene;
		
		string startFolder = "";
		
		
		
		if (currentScene.Length == 0)
		{
			SaveCurrent();
			return;
		}
		else if (currentScene.StartsWith("Assets/"))
		{
			string needResaveAs = currentScene.Substring(currentScene.IndexOf('/'));
			
			needResaveAs = needResaveAs.Replace(".unity", ".txt");
			
			needResaveAs = Application.dataPath + needResaveAs;
			
			startFolder = needResaveAs.Substring(0, needResaveAs.LastIndexOf('/'));
		}
		else
		{
			//TODO: Verify that it starts with TempScenes?
			
			startFolder = EditorHelper.GetProjectFolder() + TextScene.TempToTextSceneFile(currentScene);
			
			startFolder = startFolder.Substring(0, startFolder.LastIndexOf('/'));
		}
		
		string fileName = EditorUtility.SaveFilePanel("Save TextScene as", startFolder, "textscene", "txt");
		
		if (fileName.Length > 0)
			Save(fileName);
	}
	
    public static void SaveCurrent()
    {
        string currentScene = EditorApplication.currentScene;

		string needResaveAs = "";
		
		//Don't save over scenes not in the TempScenes-folder.
		if (currentScene.StartsWith("Assets/"))
		{
			if (!EditorUtility.DisplayDialog("Need resave", "This scene is residing in your Assets folder. For the TextScenes we need to re-save the .unity file in a temporary folder - you should from now on not be working on this scene file, but rather use the new TextScene file that will be generated next to your currently open scene.", "OK", "Hell no."))
				return;
			
			needResaveAs = currentScene.Substring(currentScene.IndexOf('/'));
			
			needResaveAs = needResaveAs.Replace(".unity", ".txt");
			
			needResaveAs = Application.dataPath + needResaveAs;
			
			
			
			if (File.Exists(needResaveAs))
			{
				string overwriteFile = currentScene.Replace(".unity", ".txt");
				
				Object o = AssetDatabase.LoadAssetAtPath(overwriteFile, typeof(TextAsset));
				
				Selection.activeObject = o;
				
				if (!EditorUtility.DisplayDialog("Overwrite?", "A file already exists at the default save position (" + overwriteFile +") - do you want to overwrite, or choose a new name?", "Overwrite", "Choose new name"))
					needResaveAs = "";
				else
					Debug.Log("Converting and overwriting scene to text: " + needResaveAs);
					
			}
			else
				Debug.Log("Converting scene to text: " + needResaveAs);
			
			
			currentScene = "";
		}
		
		
        if (currentScene.Length == 0 || needResaveAs.Length > 0)
        {	
			string startPath = Application.dataPath;
			
			string path = needResaveAs.Length > 0 ? needResaveAs : EditorUtility.SaveFilePanel("Save scene file", startPath, "newtextscene", "txt");

        		if (path.Length == 0)
            		return;
		
			currentScene = path.Replace(EditorHelper.GetProjectFolder(), "");
			
			Debug.LogWarning("Saving new scene to text: " + currentScene);
        }
		else
		{
			Debug.LogWarning("Re-saving temp scene to text: " + EditorApplication.currentScene);
		}
        
		
		string textScene = currentScene.Substring(currentScene.IndexOf('/'));
		
		
		string saveFile = textScene.Replace(".unity", ".txt");
		
		Save(Application.dataPath + saveFile);
	}

	private static int CompareTransform(Transform obj1, Transform obj2)
	{
		return string.Compare(obj1.name + obj1.position, obj2.name + obj2.position);
	}
	
	public static void Save(string filePath)
	{
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Game is running", "You cannot save in Play-mode", "OK");
            return;
        }

        
		
        Transform[] sceneObjects = Helper.GetObjectsOfType<Transform>();

		List<Transform> sortedTransforms = new List<Transform>();
		
		//Sort these to get a somewhat predictable output
        foreach (Transform o in sceneObjects)
        {
            //Only serialize root objects, children are handled by their parents
            if (o.parent == null)
                sortedTransforms.Add(o);
        }
		
		warnings = 0;
		
		sortedTransforms.Sort(CompareTransform);
		
		StringBuilder sceneText = new StringBuilder();
		
		foreach(Transform o in sortedTransforms)
			Serialize(sceneText, o.gameObject, 0);
		
		
		//TODO: If the save didn't go without warnings, show a message/confirmation
		//      dialog here.
		if (warnings > 0)
		{
			EditorUtility.DisplayDialog("ERROR: Scene not saved", "You had " + warnings + " errors or warnings during the save. Please fix them up and try again.", "OK");
			return;
		}
		
		
		StreamWriter fileStream = File.CreateText(filePath);
		fileStream.Write(sceneText.ToString());
        fileStream.Close();
		
		Debug.Log("Wrote scene to file: " + filePath);
		
		string assetPath = filePath.Replace(EditorHelper.GetProjectFolder(), "");
		
		Debug.Log("Reimporting: " + assetPath);
		
		//Import the asset.
		AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
					
		Selection.activeObject = AssetDatabase.LoadAssetAtPath(assetPath, typeof(TextAsset));
			
		TextSceneDeserializer.Load(filePath);
	}
	
	

    private static void Serialize(StringBuilder stream, GameObject go, int indent)
    {
		//FIXME: Uh-oh. Seems like I missed this the first time - I might have overcomplicated
		//       matters slightly :S Need to investigate this further.
		/*
		SerializedObject serializeObject = new SerializedObject(go);
		
		SerializedProperty seralizedProperty = serializeObject.GetIterator();
		
		do
		{
			stream.WriteLine(seralizedProperty.name + " = " + seralizedProperty.ToString());
		}while(seralizedProperty.Next(true));
		
		Component[] cList = Helper.GetComponentsInChildren<Transform>(go);
		
		foreach(Component c in cList)
		{
			stream.WriteLine("Component: " + c.GetType().ToString());
			
			serializeObject = new SerializedObject(go);
		
			seralizedProperty = serializeObject.GetIterator();
		
			do
			{
				stream.WriteLine(seralizedProperty.name + " = " + seralizedProperty.stringValue);
			}while(seralizedProperty.Next(true));
		}
		
		stream.WriteLine();
		
		return;
		*/
		
        Object prefab = EditorUtility.GetPrefabParent(go);

        if (prefab != null)
        {
			PrefabType prefabType = EditorUtility.GetPrefabType(go);
			
			if (prefabType == PrefabType.DisconnectedModelPrefabInstance
			    || prefabType == PrefabType.DisconnectedPrefabInstance)
			{
			}
			else
			{
				string path = AssetDatabase.GetAssetPath(prefab);
				
				string guid = AssetDatabase.AssetPathToGUID(path);
			
	            WriteLine(stream, indent, "prefab " + go.name);
	            WriteLine(stream, indent, "assetpath " + path + ", " + guid);
	            WriteLine(stream, indent, Vector3ToString(go.transform.localPosition));
	            WriteLine(stream, indent, QuatToString(go.transform.localRotation));
	            WriteLine(stream, indent, Vector3ToString(go.transform.localScale));
	
				if (indent == 0)
	            		WriteLine(stream, indent, "");
	
	            return;
				
			}
        }
		
		TextSceneObject tso = go.GetComponent<TextSceneObject>();
		
		if (tso)
		{
			string path = AssetDatabase.GetAssetPath(tso.textScene);
			
			if (path.Length == 0)
			{
				EditorUtility.DisplayDialog("ERROR", "TextSceneObjects must point at assets!", "OK");
				warnings++;
				return;
			}
			
			string fullPath = Helper.GetFullName(go);
			
			GameObject[] matchingObjects = Helper.FindGameObjectsFromFullName(fullPath);
			
			if (matchingObjects.Length > 1)
			{
				EditorUtility.DisplayDialog("WARNING", "There are several objects that has the path '" + fullPath + "' in the scene. It is not a good thing to let TextSceneObjects have the same path as other objects, because links within the TextSceneObjects may horribly break when you least need it.", "OK, I'll fix it");
				warnings++;
			}
			
			string guid = AssetDatabase.AssetPathToGUID(path);
			
			WriteLine(stream, indent, "textscene " + go.name);
            WriteLine(stream, indent, "assetpath " + path + ", " + guid);
            WriteLine(stream, indent, Vector3ToString(go.transform.localPosition));
	        WriteLine(stream, indent, QuatToString(go.transform.localRotation));
	        WriteLine(stream, indent, Vector3ToString(go.transform.localScale));
			
			if (indent == 0)
	            	WriteLine(stream, indent, "");
			
			return;
		}
		
		//Debug.Log("Writing object: " + go.name);

        

        WriteLine(stream, indent, "gameobject " + go.name);
		WriteLine(stream, indent, "tag " + go.tag + " layer " + go.layer);

        WriteLine(stream, indent, Vector3ToString(go.transform.localPosition));
	    WriteLine(stream, indent, QuatToString(go.transform.localRotation));
	    WriteLine(stream, indent, Vector3ToString(go.transform.localScale));

        int childCount = go.transform.GetChildCount();


        WriteLine(stream, indent, "children " + childCount);
		
		//Write out all children	, sorted.
		List<Transform> children = new List<Transform>();
		
        foreach (Transform t in go.transform)
			children.Add(t);
		
		children.Sort(CompareTransform);
			
		foreach(Transform t in children)
            Serialize(stream, t.gameObject, indent + 1);
		

		Component[] components = go.GetComponents<Component>();
		
		List<Component> componentsToWrite = new List<Component>();
		
		foreach(Component comp in components)
		{
			if (comp is Transform)
				continue;
			
			//HACK. Fix this gracefully.
			if (comp is ParticleEmitter)
			{
				EditorUtility.DisplayDialog("Warning", "The component " + comp.GetType().ToString() + " cannot be saved. You can get around this by making the object " + Helper.GetFullName(go) + " into a prefab and drop that into the scene instead", "OK");
				
				Debug.LogWarning("Skipping abstract component: " + comp.GetType().ToString(), go);
				
				warnings++;
				continue;
			}
			
			componentsToWrite.Add(comp);
		}
		
		WriteLine(stream, indent, "components " + componentsToWrite.Count);
		
		
		foreach(Component comp in componentsToWrite)
		{		
			Serialize(stream, comp, indent + 1);
		}


		if (indent == 0)
        		WriteLine(stream, indent, "");
    }

    private static string Vector3ToString(Vector3 source)
    {
        return source.x.ToString("F5") + " " + source.y.ToString("F5") + " " + source.z.ToString("F5");
    }

    private static string QuatToString(Quaternion source)
    {
        return source.x.ToString("F5") + " " + source.y.ToString("F5") + " " + source.z.ToString("F5") + " " + source.w.ToString("F5");
    }

    private static void WriteLine(StringBuilder stream, int indent, string line)
    {
        if (indent > 0)
			stream.Append(IndentString(indent));

        stream.Append(line);
		stream.Append('\n');
    }
	
	private static string IndentString(int indent)
	{
		StringBuilder sb = new StringBuilder();
		
		for (int i = 0; i < indent; i++)
            sb.Append("  ");
		
		return sb.ToString();
	}

    private static void Serialize(StringBuilder stream, Component comp, int indent)
    {
		System.Type type = comp.GetType();
		
		//Save off the fields for later, so we know beforehand how many we need to
		//read when deserializing.
		List<string> fieldList = new List<string>();
		
		
		MemberInfo[] mil = type.GetMembers(BindingFlags.Public | BindingFlags.Instance);
		
		foreach(MemberInfo mi in mil)
		{			
			FieldInfo fi = type.GetField(mi.Name);
			
			object val = null;
			string memberType = "";
			string memberName = mi.Name;
			
			
			if (fi != null)
			{
				memberType = "field";
				val = fi.GetValue(comp);
			}
			
			PropertyInfo pi = type.GetProperty(mi.Name);
			
			if (pi != null)
			{
				memberType = "property";
				
				MethodInfo getMethod = pi.GetGetMethod();
				
				//HACKs.
				if (memberName == "inertiaTensorRotation" && comp is Rigidbody)
					val = null;
				else if (memberName == "mesh" && comp is MeshFilter)
					val = null;//Debug.Log("Skipping mesh property because it will leak in edit mode");
				else if (memberName == "material" && comp is Renderer)
					val = null;//Debug.Log("Skipping renderer material property because it will leak in edit mode");
				else if (memberName == "materials" && comp is Renderer)
					val = null;//Debug.Log("Skipping renderer materials property because it will leak in edit mode");
				else if (memberName == "sharedMaterial" && comp is Renderer)
					val = null;//sharedMaterials will include this one, it's redundant.
				else if (memberName == "material" && comp is Collider)
					val = null;//Debug.Log("Skipping physics material property because it will leak in edit mode");
				else if (memberName == "mesh" && comp is Collider)
					val = null;//Debug.Log("Skipping physics mesh property because... I have no good reason.");
				else if (memberName == "particles" && comp is ParticleEmitter)
					val = null;//Debug.Log("Skipping particles property because it will change all the time if selected");
				else if (memberName == "name" || memberName == "tag" || memberName == "layer")
					val = null;
				else if (memberName == "active")
					val = null;//Deprecated, AFAIK.
				else if (memberName == "hideFlags" && comp.hideFlags == 0)
					val = null;//Skip this if it has the default value.
				else if (memberName == "enabled"
				         && getMethod != null
				         && (System.Boolean)getMethod.Invoke(comp, null) == true)
					val = null;//Skip this if it has the default value.
				//Camera properties not editable from the editor.
				else if (memberName == "pixelRect" && comp is Camera)
					val = null;
				else if (memberName == "aspect" && comp is Camera)
					val = null;
				else if (memberName == "layerCullDistances" && comp is Camera)
					val = null;
				//The reason we don't call this above, is that it will leak resources if called on "mesh" and maybe "material".
				else if (getMethod != null && pi.GetSetMethod() != null)
					val = getMethod.Invoke(comp, null);
			}
			
			
			if (val != null)
			{
				WriteComponentValue(comp, memberType, memberName, val, fieldList);
			}
		}
		
		
		WriteLine(stream, indent, type.ToString() + " " + fieldList.Count);
	
		//To correctly indent lines not directly fields (such as array entries)
		string indentString = "\n" + IndentString(indent+1);
		
		foreach(string field in fieldList)
		{
			string indentedField = field.Replace("\n", indentString);
			
			WriteLine(stream, indent+1, indentedField);
		}
	}
	
	private static void PopupInSceneLinkWarning(Component comp, string memberName, string linkedObject)
	{
		Selection.activeObject = comp.gameObject;
		
		EditorUtility.DisplayDialog("Warning", "There seems to be an unresolvable link from '" + comp.GetType().ToString() + "' on '" + comp.gameObject.name + "' ("  + memberName + " = " + linkedObject + "). This link will be lost! Once the save has completed, you can resolve the problem manually.", "OK"); 
	
		warnings++;
	}
	
	private static bool WriteComponentValue(Component comp, string memberType, string memberName, object val, List<string> fieldList)
	{
		//Debug.Log("Writing component member: " + memberName);
		
		if (val is UnityEngine.Object)
		{
			Object uo = val as Object;
			
			string assetPath = val == null ? "" : AssetDatabase.GetAssetPath(uo);
			string assetGUID = "";
			
			string uoName = uo == null ? "" : uo.name;
			
			if (assetPath.Length > 0)
				assetGUID = AssetDatabase.AssetPathToGUID(assetPath);
		
			if (assetPath.Length > 0)
			{
				fieldList.Add(memberType + " " + memberName + " asset " + val.GetType() + " = " + assetPath + ", " + uoName + ", " + assetGUID);
			}
			else if (uoName.Length > 0) 
			{
				string fullGOName = "";
				
				if (uo is Component)
					fullGOName = Helper.GetFullName((uo as Component).gameObject);
				else if (uo is GameObject)
					fullGOName = Helper.GetFullName((uo as GameObject));
					
				if (fullGOName.Length > 0)
				{
					//GameObject tmpGo = GameObject.Find(fullGOName);
			
					//if (tmpGo == null)
					//	EditorUtility.DisplayDialog("WARNING", "Unable to find: '" + fullGOName + "'", "OK");
					
					GameObject[] gos = Helper.FindGameObjectsFromFullName(fullGOName);
					
					if (gos.Length > 1)
					{
                        warnings++;

						if (!EditorUtility.DisplayDialog("WARNING", "There are several objects that will have the path '" + fullGOName + "' - please rename them so they become unique. The TextScene in-scene links rely on unique object paths", "Continue", "Stop writing this object"))
							return false;
					}
					fieldList.Add(memberType + " " + memberName + " scenelink " + val.GetType() + " = " + fullGOName);
				}
				else if (uo is Mesh)
				{
					fieldList.Add(memberType + " " + memberName + " builtinmesh " + val.GetType() + " = " + uoName);
				}
				
				else if (uo is Material)
				{
					fieldList.Add(memberType + " " + memberName + " builtinmaterial " + val.GetType() + " = " + uoName);
				}
				else
				{
					PopupInSceneLinkWarning(comp, memberName, uoName);
				
					Debug.LogWarning("In-scene links currently not supported for UnityEngine.Object: " + comp.gameObject.name + ":" + memberName + " (" + uoName + ")");
				
					return false;
				}
			}
			else	
			{	
				return false;
			}
		}
		else
		{
			System.Type valueType = val.GetType();
			
			if (valueType == typeof(System.Int32)
			    || valueType == typeof(System.Single)
			    || valueType == typeof(System.Boolean)
			    || valueType == typeof(System.String)
			    || valueType == typeof(UnityEngine.Vector2)
			    || valueType == typeof(UnityEngine.Vector3)
			    || valueType == typeof(UnityEngine.Vector4)
			    || valueType == typeof(UnityEngine.Quaternion)
			    || valueType.IsEnum)  
				fieldList.Add(memberType + " " + memberName + " primitive " + valueType.ToString() + " = " +  val.ToString());
			else if (valueType == typeof(UnityEngine.Color))
			{
				fieldList.Add(memberType + " " + memberName + " primitive " + valueType.ToString() + " = " +  val.ToString().Replace("RGBA", ""));
			}
			else if (valueType == typeof(UnityEngine.Rect))
			{
				string rectStrippedString = val.ToString();
				
				rectStrippedString = rectStrippedString.Replace("left:", "");
				rectStrippedString = rectStrippedString.Replace("width:", "");
				rectStrippedString = rectStrippedString.Replace("top:", "");
				rectStrippedString = rectStrippedString.Replace("height:", "");
				
				fieldList.Add(memberType + " " + memberName + " primitive " + valueType.ToString() + " = " +  rectStrippedString);
			}
			else if (valueType == typeof(Matrix4x4))
			{
				//'Gracefully' skip matrices.
			}
			else if (val.GetType().IsArray)
			{
				System.Array array = (val as System.Array);
				
				List<string> arrayEntryList = new List<string>();
				
				arrayEntryList.Add(memberType + " " + memberName + " array " + valueType.ToString() + " = " + array.Length);
				
				for (int i = 0; i < array.Length; i++)
				{
					if (array.GetValue(i) == null)
						arrayEntryList.Add("null");
					else
					{
						if (!WriteComponentValue(comp, " ", " ", array.GetValue(i), arrayEntryList))
						{
							arrayEntryList.Clear();
							break;
						}
					}
				}
				
				
				StringBuilder sb = new StringBuilder();
				
				for (int i = 0; i < arrayEntryList.Count; i++)
				{
					if (i == arrayEntryList.Count-1)
						sb.Append(arrayEntryList[i]);
					else
						sb.Append(arrayEntryList[i] + '\n');
				}
				
				if (sb.Length > 0)
					fieldList.Add(sb.ToString());
			}
			else
			{
				//Try to write "complex" stuff, typically structs/classes we haven't converted to seomthing
				//more handy/readable.
				try
				{
					List<string> complexEntryList = new List<string>();
					
					FieldInfo[] fields = val.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
					
					foreach(FieldInfo fi in fields)
					{
						WriteComponentValue(comp, "  field", fi.Name, fi.GetValue(val), complexEntryList);
					}
	
					
					PropertyInfo[] properties = val.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
					
					foreach(PropertyInfo pi in properties)
					{
						MethodInfo getMethod = pi.GetGetMethod();
						
						MethodInfo setMethd = pi.GetSetMethod();
						
						if (getMethod != null && setMethd != null)
						{
							//Debug.LogError(memberName + " Comp: " + comp.gameObject.name + " - " + comp.GetType().ToString() + " Prop: " + pi.Name);
							object propertyValue = getMethod.Invoke(val, null);
							
							WriteComponentValue(comp, "  property", pi.Name, propertyValue, complexEntryList);
						}
					}
					
					
					StringBuilder sb = new StringBuilder();
					
					sb.Append(memberType + " " + memberName + " complex " + valueType.ToString() + " = " + complexEntryList.Count + '\n');
					
					for (int i = 0; i < complexEntryList.Count; i++)
					{
						if (i == complexEntryList.Count-1)
							sb.Append(complexEntryList[i]);
						else
							sb.Append(complexEntryList[i] + '\n');
					}
					
					if (sb.Length > 0)
						fieldList.Add(sb.ToString());
					
					return true;
				}
				catch (System.Exception e)
				{
					warnings++;
					Debug.LogError("Failed to write: " + memberName + " on " + comp.GetType().ToString() + " on object " + comp.name + " Exception: " + e);
					return false;
				}
			}
		}
		
		return true;
	}
}