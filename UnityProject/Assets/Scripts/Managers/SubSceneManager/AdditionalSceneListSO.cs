using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "AdditionalSceneListSO", menuName = "ScriptableObjects/AdditionalSceneList", order = 1)]
public class AdditionalSceneListSO : ScriptableObject
{
	[Header("Provide the exact name of the scene in the fields below:")]
	[InfoBox("Add your additional scenes to this list for it to be " +
	         "spawned at runtime. Remember to also add your scene to " +
	         "the build settings list",EInfoBoxType.Normal)]

	public List<AssetReference> AdditionalScenes = new List<AssetReference>();


	[Tooltip("Default Central Command scene used if no specific map is set")]
	public List<AssetReference> defaultCentComScenes = new List<AssetReference>();

	[Tooltip("List of CentCom scenes that will be picked randomly at round load unless specific map is set")]
	public List<CentComData> CentComScenes = new List<CentComData>();


	[Tooltip("List of Syndie bases that will be picked randomly at round load unless specific map is set")]
	public List<AssetReference> defaultSyndicateScenes = new List<AssetReference>();

	[Tooltip("Used to set a specific scene to load for a map")]
	public List<SyndicateData> SyndicateScenes = new List<SyndicateData>();

	[Tooltip("List of wizard scenes that will be picked randomly at round load, if wizard gamemode.")]
	public List<AssetReference> WizardScenes = new List<AssetReference>();

	public List<AssetReference> WizardScenesCustom = new List<AssetReference>();


	[Serializable]
	public class CentComData
	{
		public AssetReference CentComSceneName;
		public AssetReference DependentScene = null;
	}

	[Serializable]
	public class SyndicateData
	{
		public AssetReference SyndicateSceneName;
		public AssetReference DependentScene = null;
	}
}
