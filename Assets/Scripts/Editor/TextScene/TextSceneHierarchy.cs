/// 
///  Copyright (c) 2010 TerraVision AS
///  See LICENSE file for licensing details
///
/// BUGS:	Somewhat fixed:
/// 			*Prefab instances gets renamed to their original prefab name if you load a TextScene and hit
/// 			"apply" on the prefab instance (See unity case 336621)
/// 			
/// TODO:   *Add support for multiple selected items


using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;

/// <summary>
/// Custom hierarchy view compatible with TextScene functionality (scene-in-scene links, various
/// constraints and drag'n'drop instantiation of TextScene-links).
/// </summary>
public class TextSceneHierarchy : EditorWindow
{
    enum EntryType
    {
        Normal = 0,
        PrefabChild,
        PrefabRoot,
        TextSceneChild,
        TextSceneRoot
    }

    static class EntryTypeStyle
    {
        private static Dictionary<EntryType, GUIStyle> styles = new Dictionary<EntryType, GUIStyle>();

        public static GUIStyle Get(EntryType type)
        {
            if (styles.ContainsKey(type))
                return styles[type];

            GUIStyle style = new GUIStyle("HI Label");

            if (type == EntryType.PrefabRoot)
                style.normal.textColor = new Color(0.2f, 0.2f, 0.9f);
            else if (type == EntryType.TextSceneRoot)
                style.normal.textColor = new Color(0.9f, 0.9f, 0.2f);
            else if (type == EntryType.PrefabChild)
                style.normal.textColor = new Color(0.2f, 0.2f, 0.6f);
            else if (type == EntryType.TextSceneChild)
                style.normal.textColor = new Color(0.6f, 0.6f, 0.2f);
			else
				style.normal.textColor = Color.black;

            styles.Add(type, style);

            return styles[type];
        }
    }

    static class Styles
    {
        public static GUIStyle foldout = new GUIStyle("IN Foldout");
        public static GUIStyle insertion = new GUIStyle("PR Insertion");
    }

    class Entry
    {
        private static Entry selected = null;

        private double expandedTimer = 0.0;
        public bool expanded = false;
        public GameObject go;

        private List<Entry> entries = new List<Entry>();

        private EntryType type = EntryType.Normal;

        public Entry()
        {
        }

        public Entry(GameObject go)
        {
            this.go = go;
        }

        public void AddChild(GameObject gameObject)
        {
            AddChild(gameObject, EntryType.Normal);
        }

        public bool hasChildren
        {
            get
            {
                return entries.Count > 0;
            }
        }

        private void Duplicate()
        {
            if (go == null)
                return;

            Undo.RegisterSceneUndo("Duplicate " + go.name);

            GameObject copy = null;

            GameObject prefab = EditorUtility.GetPrefabParent(go) as GameObject;

            if (prefab != null)
            {
                PrefabType type = EditorUtility.GetPrefabType(prefab);

                if (type == PrefabType.ModelPrefab || type == PrefabType.Prefab)
                {
                    copy = EditorUtility.InstantiatePrefab(prefab) as GameObject;
                    copy.transform.position = go.transform.position;
                    copy.transform.rotation = go.transform.rotation;
                    copy.transform.localScale = go.transform.localScale;
                }
            }

            if (copy == null)
                copy = GameObject.Instantiate(go, go.transform.position, go.transform.rotation) as GameObject;

            copy.transform.parent = go.transform.parent;

            copy.name = go.name + "_copy";

            Selection.activeGameObject = copy;
        }

        private void DisconnectPrefab()
        {
            if (go == null)
                return;

            GameObject copy = null;

            GameObject prefab = EditorUtility.GetPrefabParent(go) as GameObject;

            if (prefab != null)
            {
                PrefabType type = EditorUtility.GetPrefabType(prefab);

                if (type == PrefabType.ModelPrefab || type == PrefabType.Prefab)
                {
                    Undo.RegisterSceneUndo("Disconnect prefab " + go.name);

                    copy = GameObject.Instantiate(go, go.transform.position, go.transform.rotation) as GameObject;
                    copy.transform.position = go.transform.position;
                    copy.transform.rotation = go.transform.rotation;
                    copy.transform.localScale = go.transform.localScale;

                    copy.transform.parent = go.transform.parent;

                    copy.name = go.name;

                    DestroyImmediate(go);

                    Selection.activeGameObject = copy;
                }
            }
        }

        private void DisconnectTextScene()
        {
            if (go == null)
                return;

            Undo.RegisterSceneUndo("Disconnect TextScene " + go.name);

            TextSceneObject textSceneComp = go.GetComponent<TextSceneObject>();

            if (textSceneComp != null)
                DestroyImmediate(textSceneComp);

            TextSceneObject[] firstLayer = Helper.GetComponentsInChildren<TextSceneObject>(go, 1);

            //Enable next layer of TextSceneObjects.
            foreach (TextSceneObject tso in firstLayer)
            {
                tso.hideFlags = 0;
                tso.transform.hideFlags = 0;
            }


            //Enable all disconnected gameobjects until the next TextSceneObject.
            foreach (Transform child in go.transform)
            {
                Component[] toEnable = Helper.GetComponentsInChildrenAboveType<TextSceneObject>(child.gameObject);

                foreach (Component comp in toEnable)
                    comp.hideFlags = 0;
            }

            TextSceneHierarchy tsh = EditorWindow.GetWindow(typeof(TextSceneHierarchy)) as TextSceneHierarchy;
            tsh.OnHierarchyChange();
        }

        private void Delete()
        {
            if (go != null)
            {
                Undo.RegisterSceneUndo("Delete " + go.name);
                DestroyImmediate(go);
            }
        }

        public void AddChild(GameObject gameObject, EntryType entryType)
        {
            Entry entry = entries.Find(delegate(Entry x) { return x.go == gameObject; });

            if (entry == null)
            {
                entry = new Entry(gameObject);

                entries.Add(entry);
            }

            entry.type = entryType;

            if (entryType == EntryType.Normal)
            {
                if (entry.go.GetComponent<TextSceneObject>() != null)
                {
                    entry.type = EntryType.TextSceneRoot;
                }
                else if (EditorUtility.GetPrefabParent(entry.go) != null)
                {
                    entry.type = EntryType.PrefabRoot;
                }
            }
            else if (entryType == EntryType.PrefabRoot)
                entry.type = EntryType.PrefabChild;
            else if (entryType == EntryType.TextSceneRoot)
                entry.type = EntryType.TextSceneChild;

            foreach (Transform t in entry.go.transform)
                entry.AddChild(t.gameObject, entry.type);
        }

        public bool HandleDrag()
        {
            if (Event.current.type == EventType.DragUpdated 
                || Event.current.type == EventType.DragPerform)
            {
                Entry e = DragAndDrop.GetGenericData("entry") as Entry;

                //Do not allow dragging stuff onto prefabs or textscene links.
                if (this.type != EntryType.Normal)
                {
                    selected = null;
                    DragAndDrop.visualMode = DragAndDropVisualMode.None;
                    return false;
                }
                //Do not allow dragging prefab or textscene children around.
                else if (e != null 
                    && (e.type == EntryType.PrefabChild 
                    || e.type == EntryType.TextSceneChild))
                {
                    selected = null;
                    DragAndDrop.visualMode = DragAndDropVisualMode.None;
                    return false;
                }
                else
                {
                    selected = this;
                    
                    if (expanded == false 
                        && EditorApplication.timeSinceStartup > this.expandedTimer)
                        this.expanded = true;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;

                    if (Event.current.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        if (e != null && e.go != null)
                        {
                            GameObject dragged = e.go;

                            if (dragged != null)
                            {
                                if (dragged != go)
                                {
                                    Undo.RegisterSceneUndo("Move " + dragged.name);

                                    //If the target is null (the root), we might want to orphan the object.
                                    if (go == null)
                                    {
                                        if (dragged.transform.parent != null)
                                        {
                                            if (EditorUtility.DisplayDialog("Orphan", "Do you want to make " + dragged.name + " an orphan?", "Yes", "No"))
                                            {
                                                dragged.transform.parent = null;
                                                Selection.activeGameObject = dragged;
												TextSceneHierarchy.Refresh();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //Change parent to whatever we're hovering above at the time of drag end.
                                        if (EditorUtility.DisplayDialog("Change parent", "Do you want to make " + go.name + " the parent of " + dragged.name + "?", "Yes", "No"))
                                        {
                                            dragged.transform.parent = go.transform;
                                            Selection.activeGameObject = dragged;
											TextSceneHierarchy.Refresh();
                                        }
                                    }
                                }
                            }
                        }
                        else if (DragAndDrop.objectReferences.Length > 0)
                        {
                            UnityEngine.Object dragged = DragAndDrop.objectReferences[0];

							MonoScript comp = dragged as MonoScript;
							GameObject draggedGO = dragged as GameObject;
							TextAsset draggedTextScene = dragged as TextAsset;
							
							if (comp != null)
							{
								if (this.go != null)
								{
                                    this.go.AddComponent(comp.GetClass());
                                    Selection.activeGameObject = this.go;
								}
							}
                            else if (draggedTextScene != null)
                            {
                                Undo.RegisterSceneUndo("Add scene " + draggedTextScene.name);

                                TextSceneDeserializer.LoadAdditive(draggedTextScene, this.go);
                            }
                            else if (draggedGO != null)
                            {
                                PrefabType pt = EditorUtility.GetPrefabType(dragged);

                                if (pt == PrefabType.ModelPrefab || pt == PrefabType.Prefab)
                                {
                                    Undo.RegisterSceneUndo("Create " + draggedGO.name);

                                    GameObject instance = EditorUtility.InstantiatePrefab(dragged) as GameObject;

                                    if (this.go != null)
                                        instance.transform.parent = this.go.transform;

                                    instance.transform.localPosition = Vector3.zero;
                                    instance.transform.localRotation = Quaternion.identity;
									instance.name = instance.name + "_instance";
									
									Selection.activeGameObject = instance;
                                }
                            }
                        }
                    }

                    
                }

                Event.current.Use();
                return true;
            }

            return false;
        }

        public void OnGUI(int level, ref float offset)
        {
            float startOffset = offset;

            GUIStyle style = EntryTypeStyle.Get(type);

            if (go != null)
            {
                Rect lineRect = new Rect(0, offset, 1000, style.lineHeight);
                Rect labelRect = new Rect(level * 20.0f, offset, 1000, style.lineHeight);
                Rect expandRect = new Rect(level * 20.0f, offset, 20, style.lineHeight);

                offset += style.lineHeight;

				

                if (Event.current.type == EventType.repaint && selected == this)
				{	
                    style.Draw(lineRect, false, true, true, false);
				}

                if (hasChildren)
                    expanded = GUI.Toggle(expandRect, expanded, GUIContent.none, Styles.foldout);

                GUI.Label(labelRect, go.name, style);
				
				//FIXME: This is to get the scrollbar to work :S
				GUILayout.BeginHorizontal();
				GUILayout.Space(level * 20.0f);
				GUILayout.Label("", style);
				GUILayout.EndHorizontal();
            
            }

            if (go == null || expanded)
            {
                foreach (Entry e in entries)
                    e.OnGUI(level + 1, ref offset);
            }

            Rect selectRect = new Rect(0, startOffset + style.lineHeight * 0.1f, 200, offset-startOffset - style.lineHeight * 0.1f);

            //Base entry
            if (go == null)
                selectRect.height = float.MaxValue;

            if (selectRect.Contains(Event.current.mousePosition))
            {
                if (go != null)
                {
                    if (Event.current.type == EventType.MouseDown)
                    {
                        if (Event.current.button == 1)
                        {
                            EditorGUIUtility.PingObject(go);

                            GenericMenu menu = new GenericMenu();

                            if (type == EntryType.Normal
                            || type == EntryType.PrefabRoot
                            || type == EntryType.TextSceneRoot)
                            {
                                menu.AddItem(EditorHelper.TextContent("Duplicate"), false, new GenericMenu.MenuFunction(this.Duplicate));
                                menu.AddItem(EditorHelper.TextContent("Delete"), false, new GenericMenu.MenuFunction(this.Delete));

                                if (type == EntryType.PrefabRoot)
                                {
                                    menu.AddSeparator("");
                                    menu.AddItem(EditorHelper.TextContent("Disconnect"), false, new GenericMenu.MenuFunction(this.DisconnectPrefab));
                                }
                                else if (type == EntryType.TextSceneRoot)
                                {
                                    menu.AddSeparator("");
                                    menu.AddItem(EditorHelper.TextContent("Disconnect"), false, new GenericMenu.MenuFunction(this.DisconnectTextScene));
                                }
                                else if (type == EntryType.Normal)
                                {
                                    menu.AddSeparator("");
                                    menu.AddItem(EditorHelper.TextContent("Create Empty"), false,
                                                 new GenericMenu.MenuFunction(delegate
                                                 {
                                                     GameObject empty = new GameObject();
                                                     empty.transform.parent = go.transform;
                                                     empty.transform.localPosition = Vector3.zero;
                                                     empty.transform.localRotation = Quaternion.identity;
                                                 }));
                                }
                            }
                            else
                                menu.AddItem(EditorHelper.TextContent("No options"), false, null);

                            menu.ShowAsContext();
                        }
                        else if (Event.current.clickCount == 2)
                            expanded = !expanded;

                        Event.current.Use();
                    }
                    else if (Event.current.type == EventType.MouseUp)
                    {
                        if (Event.current.clickCount == 1)
                        {
                            Selection.activeGameObject = go;

                            TextSceneObject tso = go.GetComponent<TextSceneObject>();

                            if (tso != null)
                                EditorGUIUtility.PingObject(tso.textScene);

                            Object prefabParent = EditorUtility.GetPrefabParent(go);

                            if (prefabParent != null)
                                EditorGUIUtility.PingObject(prefabParent);
                        }

                        Event.current.Use();
                    }
                    else if (Event.current.type == EventType.MouseDrag)
                    {
                        DragAndDrop.PrepareStartDrag();

                        UnityEngine.Object[] draggedGO = new Object[1];

                        draggedGO[0] = go;


                        DragAndDrop.objectReferences = draggedGO;
                        DragAndDrop.SetGenericData("entry", this);
                        DragAndDrop.StartDrag("Dragging " + go.name);

                        Event.current.Use();
                    }
                }

                HandleDrag();
            }
            else
            {
                //Reset expand timer if the cursor moves out of the entry rect.
                expandedTimer = EditorApplication.timeSinceStartup + 0.3;
            }

            if (selected == this)
            {
                if (go != null)
                {
                    if (Event.current.type == EventType.KeyDown)
                    {
                        if (Event.current.keyCode == KeyCode.Delete)
                        {
                            this.Delete();
                        }
                        //FIXME: Use control/command, not shift.
                        else if (Event.current.shift && Event.current.keyCode == KeyCode.D)
                        {
                            this.Duplicate();
                        }
                    }
                }
            }
        }

        public bool FindAndTagSelected(GameObject gameObject)
        {
            if (go == gameObject)
            {
                selected = this;
                return true;
            }

            foreach (Entry e in entries)
            {
                if (e.FindAndTagSelected(gameObject))
                {
                    expanded = true;
                    return true;
                }
            }

            return false;
        }

        internal void Sort()
        {
            entries.Sort(delegate(Entry x, Entry y) { return string.Compare(x.go.name, y.go.name); });
        }

        internal void FillExpanded(List<GameObject> expandedObjects)
        {
            if (expanded)
                expandedObjects.Add(go);

            foreach (Entry e in entries)
                e.FillExpanded(expandedObjects);
        }

        internal void Expand(GameObject gameObject)
        {
            if (go == gameObject)
            {
                expanded = true;
                return;
            }

            foreach (Entry e in entries)
                e.Expand(gameObject);
        }
    }

    Entry root = new Entry();

    private Vector2 scroll = new Vector2();
	
	private double nextCheckForDefaultHierarchy = 0.0f;

	private static bool CheckForDefaultHierarchy()
	{
		Assembly editorAssembly = Assembly.GetAssembly(typeof(EditorWindow));
        
        System.Type hierarchyWindowType = editorAssembly.GetType("UnityEditor.HierarchyWindow");

        Object[] window = Resources.FindObjectsOfTypeAll(hierarchyWindowType);

        if (window.Length > 0)
        {
            if (EditorUtility.DisplayDialog("Default Hierarchy", "The default hierarchy window seems to be up. It is recommended that this one is not used, as it lets you do a few things regarding TextSceneObjects that the system will not correctly pick up (such as moving TextScene subobjects out of their parent). The default hierarchy do have a lot of handy functionality, though, so you can feel free to ignore this message if you know what the limitations of the TextScene system is. You can turn off future warnings by disabling it in the TextScene menu item.", "Close default hierarchy", "Ignore, I know the limitations!"))
            {
                foreach (EditorWindow w in window)
				{
					if (w != null)
                   		w.Close();
				}
            }
			else
				return false;
        }
		
		return true;
	}
	
    public static void CreateHierarchy()
    {
		CheckForDefaultHierarchy();
		
        TextSceneHierarchy tsh = EditorWindow.GetWindow(typeof(TextSceneHierarchy)) as TextSceneHierarchy;

        tsh.Show();
        tsh.OnHierarchyChange();
    }

	protected void OnEnable()
	{
		nextCheckForDefaultHierarchy = EditorApplication.timeSinceStartup + 2.0f;
		OnHierarchyChange();
	}
	
	protected void Update()
	{
		//Redundant update. FIXME: Necessary?
		TextSceneMonitor.MonitorUpdate();
		
		if (!EditorApplication.isPlaying && EditorApplication.timeSinceStartup > nextCheckForDefaultHierarchy)
		{
			if (PlayerPrefs.GetInt("ShowDefaultHierarchyWarning", 1) > 0)
			{
				if (CheckForDefaultHierarchy())
					nextCheckForDefaultHierarchy = EditorApplication.timeSinceStartup + 2.0f;
				else
					nextCheckForDefaultHierarchy = double.MaxValue;
			}
			
			
		}
	}
	
	public static void Refresh()
	{
		TextSceneHierarchy tsh = EditorWindow.GetWindow(typeof(TextSceneHierarchy)) as TextSceneHierarchy;
		tsh.OnHierarchyChange();
	}
	
    protected void OnHierarchyChange()
    {	
        Transform[] transforms = Helper.GetObjectsOfType<Transform>();

        List<GameObject> expanded = new List<GameObject>();

        root.FillExpanded(expanded);

        root = new Entry();

        foreach (Transform t in transforms)
        {
            if (t.parent == null)
                root.AddChild(t.gameObject);
        }

        foreach (GameObject go in expanded)
            root.Expand(go);

        root.FindAndTagSelected(Selection.activeGameObject);

        root.Sort();

		Repaint();
    }

    protected void OnSelectionChange()
    {
        root.FindAndTagSelected(Selection.activeGameObject);

        Repaint();
    }

    protected void OnGUI()
    {
        float offset = 0.0f;

        scroll = GUILayout.BeginScrollView(scroll);
        GUILayout.BeginVertical();
        root.OnGUI(-1, ref offset);
        GUILayout.EndVertical();
        GUILayout.EndScrollView();
    }
}