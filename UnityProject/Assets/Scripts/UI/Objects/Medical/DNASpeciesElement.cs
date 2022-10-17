using System;
using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UI.Core.NetUI;
using UI.Objects.Medical;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class DNASpeciesElement : DynamicEntry
{

	public PlayerHealthData PlayerHealthData;

	public Transform BodyPartContainer;

	public List<BodyPart> StoredBodyParts = new List<BodyPart>();
	public List<GameObject> SpawnedBodyParts = new List<GameObject>();

	[FormerlySerializedAs("NetSyncString")] public NetServerSyncString netServerSyncString;


	public NetClientSyncString netClientSyncString;

	public GameObject SubPrefab;

	public GUI_DNAConsole GUI_DNAConsole;

	public Transform WindowPanel;


	public void GenerateOption(string BodyPartName)
	{
		if (GUI_DNAConsole != null && GUI_DNAConsole.IsMasterTab)
		{
			//Look up
			Logger.LogError(BodyPartName);

			foreach (var Part in StoredBodyParts)
			{
				if (Part.name == BodyPartName)
				{
					GUI_DNAConsole.GenerateSpeciesTarget(Part.gameObject, PlayerHealthData);
					CloseSection();
					return;
				}
			}
		}
	}



	public void CloseSection()
	{
		WindowPanel.SetActive(false);
	}
	public void OpenSection()
	{
		if (WindowPanel.gameObject.activeSelf)
		{
			WindowPanel.SetActive(false);
		}
		else
		{
			WindowPanel.SetActive(true);
		}

	}

	public void Awake()
	{
		netServerSyncString.OnChange.AddListener(TargetSpecies);
		netClientSyncString.OnChange.AddListener(GenerateOption);

	}


	public void TargetSpecies(string SpeciesName)
	{
		if (RaceSOSingleton.TryGetRaceByName(SpeciesName, out PlayerHealthData))
		{
			StoredBodyParts.Clear();
			foreach (var gameObject in SpawnedBodyParts)
			{
				Destroy(gameObject);
			}


			foreach (var Part in PlayerHealthData.Base.Torso.Elements) //See this code, if you don't like it then fix it yourself, This is what quick implementing is about
			{
				var BodyPart = Part.GetComponent<BodyPart>();
				StoredBodyParts.Add(BodyPart);
				RecursivePopulate(BodyPart);
			}

			foreach (var Part in PlayerHealthData.Base.Head.Elements)
			{
				var BodyPart = Part.GetComponent<BodyPart>();
				StoredBodyParts.Add(BodyPart);
				RecursivePopulate(BodyPart);
			}

			foreach (var Part in PlayerHealthData.Base.ArmLeft.Elements)
			{
				var BodyPart = Part.GetComponent<BodyPart>();
				StoredBodyParts.Add(BodyPart);
				RecursivePopulate(BodyPart);
			}

			foreach (var Part in PlayerHealthData.Base.ArmRight.Elements)
			{
				var BodyPart = Part.GetComponent<BodyPart>();
				StoredBodyParts.Add(BodyPart);
				RecursivePopulate(BodyPart);
			}

			foreach (var Part in PlayerHealthData.Base.LegLeft.Elements)
			{
				var BodyPart = Part.GetComponent<BodyPart>();
				StoredBodyParts.Add(BodyPart);
				RecursivePopulate(BodyPart);
			}

			foreach (var Part in PlayerHealthData.Base.LegRight.Elements)
			{
				var BodyPart = Part.GetComponent<BodyPart>();
				StoredBodyParts.Add(BodyPart);
				RecursivePopulate(BodyPart);
			}

			foreach (var StoredBodyPart in StoredBodyParts)
			{
				var  newOb =  Instantiate(SubPrefab, BodyPartContainer);
				var img = newOb.GetComponentInChildren<SpriteHandler>();
				var  button =  newOb.GetComponent<DNAButtonData>();
				button.BodyPartName = StoredBodyPart.name;
				button.RelatedDNASpeciesElement = this;
				img.SetSpriteSO(StoredBodyPart.GetComponentInChildren<SpriteHandler>().PresentSpritesSet);
				SpawnedBodyParts.Add(newOb);
			}
		}
	}

	public void SetValues(PlayerHealthData InPlayerHealthData, GUI_DNAConsole  InGUI_DNAConsole)
	{
		PlayerHealthData = InPlayerHealthData;
		netServerSyncString.SetValue(InPlayerHealthData.name);
		GUI_DNAConsole = InGUI_DNAConsole;
		TargetSpecies(InPlayerHealthData.name);
	}

	public void RecursivePopulate(BodyPart BodyPart)
	{
		var BodyParts =  BodyPart.OrganStorage.Populater.GetFirstLayerDeprecatedAndNew();

		foreach (var bodyPart in BodyParts)
		{
			var Part_body = bodyPart.GetComponent<BodyPart>();
			StoredBodyParts.Add(Part_body);
			RecursivePopulate(Part_body);
		}

	}

}
