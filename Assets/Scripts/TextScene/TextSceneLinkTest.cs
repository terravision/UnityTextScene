/// 
///  Copyright (c) 2010 TerraVision AS
///  See LICENSE file for licensing details
///

using UnityEngine;

class TextSceneLinkTest : MonoBehaviour
{
    public Material[] materialList;
    public GameObject goLink;
    public Transform transformLink;
    public GameObject prefabLink;
    public BoxCollider colliderLink;

    public string nextScene;

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