using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "AdditionalSceneListSO", menuName = "ScriptableObjects/AdditionalSceneList", order = 1)]
public class AdditionalSceneListSO : ScriptableObject
{
	[Header("Provide the exact name of the scene in the fields below:")]
	[InfoBox("Add your additional scenes to this list for it to be " +
	         "spawned at runtime. Remember to also add your scene to " +
	         "the build settings list",EInfoBoxType.Normal)]
	public List<string> AdditionalScenes = new List<string>();

	public string defaultCentComScene;
	public List<CentComData> CentComScenes = new List<CentComData>();

	[Serializable]
	public class CentComData
	{
		public string CentComSceneName;
		public string DependentScene = null;
	}
}
