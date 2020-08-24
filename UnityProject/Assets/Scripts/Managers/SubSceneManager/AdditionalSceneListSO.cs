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

	[Tooltip("Default Central Command scene used if no specific map is set")]
	public List<string> defaultCentComScenes = new List<string>();

	[Tooltip("List of CentCom scenes that will be picked randomly at round load unless specific map is set")]
	public List<CentComData> CentComScenes = new List<CentComData>();

	[Tooltip("List of Syndie bases that will be picked randomly at round load unless specific map is set")]
	public List<string> defaultSyndicateScenes = new List<string>();

	[Tooltip("Used to set a specific scene to load for a map")]
	public List<SyndicateData> SyndicateScenes = new List<SyndicateData>();

	[Serializable]
	public class CentComData
	{
		public string CentComSceneName;
		public string DependentScene = null;
	}

	[Serializable]
	public class SyndicateData
	{
		public string SyndicateSceneName;
		public string DependentScene = null;
	}
}
