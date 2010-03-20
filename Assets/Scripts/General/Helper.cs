/// 
///  Copyright (c) 2010 TerraVision AS
///  See LICENSE file for licensing details
///

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;

public class Helper
{
    public static float SolveQuadricGetMax(float a, float b, float c)
    {
        float x1 = 0.0f;
        float x2 = 0.0f;

        SolveQuadric(a, b, c, ref x1, ref x2);

        return Mathf.Max(x1, x2);
    }

    public static int SolveQuadric(float a, float b, float c, ref float x1, ref float x2)
    {
        float discriminant = (b * b) - 4 * a * c;

        float denom = 2 * a;

        if (Mathf.Abs(discriminant) < 0.0001f)
        {
            x1 = -b / denom;
            x2 = x1;
            return 1;
        }
        else if (discriminant > 0.0f)
        {
            float sq = Mathf.Sqrt(discriminant);

            x1 = (-b + sq) / denom;
            x2 = (-b - sq) / denom;

            return 2;
        }
        else
            return 0;
        
    }

	public static T GetObjectOfType<T>() where T : Component
	{
		UnityEngine.Object[] comps = GameObject.FindObjectsOfType(typeof(T));
		
		if (comps == null || comps.Length == 0)
			return default(T);
		else
			return comps[0] as T;
	}

    public static T GetObjectOfTypeByName<T>(string name) where T : Component
	{
		UnityEngine.Object[] comps = GameObject.FindObjectsOfType(typeof(T));

        if (comps == null || comps.Length == 0)
            return default(T);
        else
        {
            for (int i = 0; i < comps.Length; i++)
            {
                if (comps[i].name == name)
                    return comps[i] as T;
            }
        }

        return default(T);
	}
	
	public static T[] GetObjectsOfType<T>() where T : Component
	{
		UnityEngine.Object[] comps = GameObject.FindObjectsOfType(typeof(T));
		
		if (comps == null || comps.Length == 0)
			return new T[0];
		else
		{
			T[] result = new T[comps.Length];
		
			for (int i = 0; i < comps.Length; i++)
				result[i] = comps[i] as T;
			
			return result;
		}
	}
	
	public static GameObject[] GetGameObjectsByName(string name)
	{
		List<UnityEngine.Object> transforms = new List<UnityEngine.Object>(GameObject.FindObjectsOfType(typeof(Transform)));
		
		return transforms.FindAll(t => t.name == name).ConvertAll(tr => (tr as Transform).gameObject).ToArray();
	}

    public static T[] GetGameObjectsByTypeAndName<T>(string name) where T : Component
    {
        UnityEngine.Object[] comps = GameObject.FindObjectsOfType(typeof(T));

        if (comps == null || comps.Length == 0)
            return new T[0];
        else
        {
            List<T> filtered = new List<T>();

            for (int i = 0; i < comps.Length; i++)
            {
                if (comps[i].name == name)
                    filtered.Add(comps[i] as T);
            }

            return filtered.ToArray();
        }
    }
	
	public static T AddComponent<T>(GameObject go) where T : Component
	{
		if (go == null)
			return null;
		
		T comp = go.GetComponent(typeof(T)) as T;
		
		if (comp == null)
			comp = go.AddComponent(typeof(T)) as T;
		
		return comp;
	}

    public static T[] AddComponent<T>(GameObject[] gameObjects) where T : Component
    {
        if (gameObjects == null)
            return new T[0];

        List<T> added = new List<T>();

        for (int i = 0; i < gameObjects.Length; i++)
        {
            T comp = gameObjects[i].AddComponent(typeof(T)) as T;

            added.Add(comp);
        }

        return added.ToArray();
    }
	
	public static void AddComponent(GameObject[] gameObjects, Type component)
	{
		if (gameObjects == null)
			return;
		
		for (int i = 0; i < gameObjects.Length; i++)
			gameObjects[i].AddComponent(component);
	}

    public static void AddComponent<T, R>(T[] gameObjects) where T : Component where R : Component
    {
        if (gameObjects == null)
            return;
		
        for (int i = 0; i < gameObjects.Length; i++)
            gameObjects[i].gameObject.AddComponent(typeof(R));
    }
	
	public static T[] AddComponentToType<T, R>(GameObject go) where T : Component where R : Component
    {
        if (go == null)
            return new T[0];
		
		List<T> added = new List<T>();

        R[] comp = Helper.GetComponentsInChildren<R>(go);
        
		for(int i = 0; i < comp.Length; i++)
		{
			added.Add(comp[i].gameObject.AddComponent(typeof(T)) as T);
		}
		
		return added.ToArray();
    }

    /// <summary>
    /// Returns all components in a hierarchy until it hits the specified type.
    /// </summary>
    public static Component[] GetComponentsInChildrenAboveType<T>(GameObject go) where T : Component
    {
        if (go.GetComponent<T>() != null)
            return new Component[0];
        

        List<Component> components = new List<Component>();

        foreach (Transform child in go.transform)
        {
            components.AddRange(GetComponentsInChildrenAboveType<T>(child.gameObject));
        }

        components.AddRange(go.GetComponents<Component>());

        return components.ToArray();
    }

    /// <summary>
    /// Returns components recursively "layers" deep. If you have a hierarchy
    /// of three parented colliders, and send in "2" for layers, you will get
    /// the two first components, but not the third.
    /// </summary>
    public static T[] GetComponentsInChildren<T>(GameObject go, int layers) where T : Component
    {
        if (layers == 0)
            return new T[0];

        List<T> components = new List<T>();

        T[] layerComps = go.GetComponents<T>();

        if (layerComps.Length > 0)
        {
            components.AddRange(layerComps);
            layers--;
        }

        foreach (Transform child in go.transform)
        {
            components.AddRange(GetComponentsInChildren<T>(child.gameObject, layers));
        }

        return components.ToArray();
    }
	
	public static T GetComponentInChildren<T>(GameObject go) where T : Component
	{
		return go.GetComponentInChildren(typeof(T)) as T;
	}
	
	public static T[] GetComponentsInChildren<T>(GameObject go) where T : Component
	{
		if (go == null)
			return new T[0];
	
		Component[] comps = go.GetComponentsInChildren(typeof(T));
		
		if (comps == null || comps.Length == 0)
			return new T[0];
		
		
		T[] result = new T[comps.Length];
		
		for (int i = 0; i < comps.Length; i++)
			result[i] = comps[i] as T;
		
		
		//WTF FIXME: What's wrong with this one?
		//result = System.Array.ConvertAll(comps, comp => (T)comp);
		
		return result;
	}
	
	public static void SetLayerRecursively(GameObject go, int layer)
	{
		go.layer = layer;
		
		foreach(Transform child in go.transform)
		{
			SetLayerRecursively(child.gameObject, layer);
		}
	}


    public static void DestroyComponentsInChildren<T>(GameObject go, bool immediate)
    {
        Component[] comps = go.GetComponentsInChildren(typeof(T));

        for (int i = 0; i < comps.Length; i++)
        {
            if (immediate)
                GameObject.DestroyImmediate(comps[i]);
            else
                GameObject.Destroy(comps[i]);
        }
    }

    public static void Destroy(GameObject[] gos)
    {
        if (gos == null)
            return;

        for (int i = 0; i < gos.Length; i++)
        {
            GameObject.Destroy(gos[i]);
        }
    }

    public static Bounds CalculateBounds(GameObject go)
    {
        Bounds b = new Bounds();

        Renderer[] renderers = Helper.GetComponentsInChildren<Renderer>(go);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (i == 0)
                b = renderers[i].bounds;
            else
                b.Encapsulate(renderers[i].bounds);
        }

        return b;
    }

    public static T AddComponentToNamedObject<T>(string name) where T : Component
    {
        GameObject go = GameObject.Find(name);

        if (go == null)
            return default(T);

        return go.AddComponent(typeof(T)) as T;
    }
	
	public static T[] AddComponentToNamedObjects<T>(string name) where T : Component
    {
        UnityEngine.Object[] objects = GameObject.FindObjectsOfType(typeof(Transform));

        List<T> filtered = new List<T>();

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i].name.Equals(name))
                filtered.Add((objects[i] as Transform).gameObject.AddComponent(typeof(T)) as T);
        }

        return filtered.ToArray();
    }

    public static T[] AddComponentToPartiallyNamedObjects<T>(string partialName) where T : Component
    {
        UnityEngine.Object[] objects = GameObject.FindObjectsOfType(typeof(Transform));

        List<T> filtered = new List<T>();

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i].name.Contains(partialName))
                filtered.Add((objects[i] as Transform).gameObject.AddComponent(typeof(T)) as T);
        }

        return filtered.ToArray();
    }
	
	public static T CreateObject<T>(Vector3 position, Quaternion rotation, Vector3 scale) where T : Component
	{
		T instance = CreateObject<T>();
		instance.transform.position = position;
		instance.transform.rotation = rotation;
		instance.transform.localScale = scale;
		
		return instance;
	}
	
	public static T CreateObject<T>() where T : Component
	{
		GameObject go = new GameObject();
        go.name = typeof(T).ToString();
		return go.AddComponent(typeof(T)) as T;
	}

    public static GameObject[] FindObjectsWithPartialName(string partialName)
    {
        UnityEngine.Object[] objects = GameObject.FindObjectsOfType(typeof(Transform));

        List<GameObject> filtered = new List<GameObject>();

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i].name.Contains(partialName))
                filtered.Add((objects[i] as Transform).gameObject);
        }

        return filtered.ToArray();
    }
	
	public static void DisableScriptsInScene<T>() where T : MonoBehaviour
	{
		T[] comps = Helper.GetObjectsOfType<T>();
		
		for(int i = 0; i < comps.Length; i++)
			comps[i].enabled = false;
	}
	
	public static GameObject FindOrFail(string name)
	{
		GameObject go = GameObject.Find(name);
		
		if (go == null)
		{
			Debug.LogError("Unable to find object: " + name);
			Debug.Break();
		}
		
		return go;
	}

    public static GameObject FindInChildren(GameObject go, string name)
    {
        foreach (Transform child in go.transform)
        {
            if (child.name == name)
                return child.gameObject;

            GameObject ret = FindInChildren(child.gameObject, name);

            if (ret != null)
                return ret;
        }

        return null;
    }
	
	public static string GetFullName(GameObject go)
	{
		
		
		Transform current = go.transform;
		
		List<Transform> parentList = new List<Transform>();
		
		while(current != null)
		{
			parentList.Add(current);
			
			current = current.parent;
		}
		
		parentList.Reverse();
		
		StringBuilder sb = new StringBuilder();
		
		foreach(Transform t in parentList)
		{
			sb.Append('/');
			sb.Append(t.name);
		}
		
		return sb.ToString();
	}
	
	public static GameObject[] FindGameObjectsFromFullName(string fullName)
	{
		Transform[] transforms = Helper.GetObjectsOfType<Transform>();
		
		
		List<GameObject> matched = new List<GameObject>();
		
		foreach(Transform t in transforms)
		{
			if (t.parent == null)
			{
				//Debug.Log("Finding full: " + fullName);
				FindMatching(t, matched, fullName);
			}
		}
		
		return matched.ToArray();
	}
	
	private static void FindMatching(Transform t, List<GameObject> matched, string remaining)
	{
		string currentLevelName = remaining.Substring(1);
		
		int separatorIndex = currentLevelName.IndexOf('/');
		
		if (separatorIndex > 0)
			remaining = currentLevelName.Substring(separatorIndex);
		else
			remaining = "";
		
		if (separatorIndex > 0)
			currentLevelName = currentLevelName.Substring(0, separatorIndex);
		
		//Debug.Log("matching object: '" + currentLevelName + "' (" + remaining + ")");
		
		if (t.name == currentLevelName)
		{
			if (remaining.Length == 0)
			{
				//Debug.Log("MATCH");
				matched.Add(t.gameObject);
			}
			else
			{
				foreach(Transform child in t)
				{
					FindMatching(child, matched, remaining);
				}
			}
		}
	}
}

