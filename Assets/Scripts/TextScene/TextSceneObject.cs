/// 
///  Copyright (c) 2010 TerraVision AS
///  See LICENSE file for licensing details
///
///  TODO: 	It should not be necessary to hold a reference to the actual asset, path and/or GUID
/// 			should be sufficient. This way it shouldn't be included in a final build as a
/// 			dependency.

using UnityEngine;

public class TextSceneObject : MonoBehaviour
{
	public TextAsset textScene;
}

