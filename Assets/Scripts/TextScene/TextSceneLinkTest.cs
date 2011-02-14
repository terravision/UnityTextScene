/// 
///  Copyright (c) 2010 TerraVision AS
///  See LICENSE file for licensing details
///

using UnityEngine;

class TextSceneLinkTest : MonoBehaviour
{
    public Material[] materialList = null;
    public GameObject goLink = null;
    public Transform transformLink = null;
    public GameObject prefabLink = null;
    public BoxCollider colliderLink = null;

    public string nextScene = null;

    protected void OnGUI()
    {
        if (GUI.Button(new Rect(20.0f, 20.0f, 100.0f, 20.0f), "Change scene"))
        {
            if (nextScene != null && nextScene.Length > 0)
                Application.LoadLevel(nextScene);
            else
                Application.LoadLevel(Application.loadedLevelName);
        }
    }
}