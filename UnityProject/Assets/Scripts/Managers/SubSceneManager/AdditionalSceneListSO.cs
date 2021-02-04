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

	[Scene]
	public List<string> AdditionalScenes = new List<string>();

	[Scene]
	[Tooltip("Default Central Command scene used if no specific map is set")]
	public List<string> defaultCentComScenes = new List<string>();

	[Tooltip("List of CentCom scenes that will be picked randomly at round load unless specific map is set")]
	public List<CentComData> CentComScenes = new List<CentComData>();

	[Scene]
	[Tooltip("List of Syndie bases that will be picked randomly at round load unless specific map is set")]
	public List<string> defaultSyndicateScenes = new List<string>();

	[Tooltip("Used to set a specific scene to load for a map")]
	public List<SyndicateData> SyndicateScenes = new List<SyndicateData>();

	[Scene]
	[Tooltip("List of wizard scenes that will be picked randomly at round load, if wizard gamemode.")]
	public List<string> WizardScenes = new List<string>();

	[Serializable]
	public class CentComData
	{
		[Scene] public string CentComSceneName;
		[Scene] public string DependentScene = null;
	}

	[Serializable]
	public class SyndicateData
	{
		[Scene] public string SyndicateSceneName;
		[Scene] public string DependentScene = null;
	}
}
