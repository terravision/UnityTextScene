/// 
///  Copyright (c) 2010 TerraVision AS
///  See LICENSE file for licensing details
///

using UnityEngine;
using System.Collections;

public class MyColor : MonoBehaviour {

	public Color color = Color.red;
	
	// Use this for initialization
	void Start () {
		renderer.material.color = color;
	}
	

}
