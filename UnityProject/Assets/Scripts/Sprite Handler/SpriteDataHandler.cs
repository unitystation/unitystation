using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility = UnityEngine.Networking.Utility;
using Mirror;
using UnityEditor;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine.Serialization;
/// <summary>
/// Stores and Process SpriteData
/// To be used in SpriteHandler
/// </summary>
[ExecuteInEditMode]
public class SpriteDataHandler : MonoBehaviour
{
	void Update() {
		var bob = gameObject;
		DestroyImmediate(this);
		EditorUtility.SetDirty(bob);
	}
}