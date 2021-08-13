using System;
using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

public class RootBodyPartController : NetworkBehaviour
{
	[SyncVar(hook = nameof(UpdateCustomisations))]
	public string PlayerSpritesData = "";

	[SyncVar(hook = nameof(UpdateChildren))]
	private string SerialisedPrefabIDs = "";

	public List<RootBodyPartContainer> RootBodyParts = new List<RootBodyPartContainer>();

	private Dictionary<string, RootBodyPartContainer> InternalRootBodyParts =
		new Dictionary<string, RootBodyPartContainer>();

	public Dictionary<string, List<RootBodyPartContainer.intName>> DictionaryToSerialised = new Dictionary<string, List<RootBodyPartContainer.intName>>();

	public PlayerSprites playerSprites;

	public void Awake()
	{
		foreach (var BParts in RootBodyParts)
		{
			BParts.RootBodyPartController = this;
			InternalRootBodyParts[BParts.name] = BParts;
		}
	}

	public void RequestUpdate(RootBodyPartContainer RootBodyPartContainer)
	{

		foreach (var intName in RootBodyPartContainer.InternalNetIDs)
		{
			intName.ClothingHide = intName.RelatedSprite.ClothingHide;
			intName.Data = intName.RelatedSprite.Data;
		}

		DictionaryToSerialised[RootBodyPartContainer.name] = RootBodyPartContainer.InternalNetIDs;
		SerialisedPrefabIDs = JsonConvert.SerializeObject(DictionaryToSerialised);
	}


	public void UpdateChildren(string InOld, string InNew)
	{
		if (isServer) return;
		SerialisedPrefabIDs = InNew;
		playerSprites.Addedbodypart.Clear();
		DictionaryToSerialised = JsonConvert.DeserializeObject<Dictionary<string, List<RootBodyPartContainer.intName>>>(SerialisedPrefabIDs);
		foreach (var Entry in DictionaryToSerialised)
		{
			if (InternalRootBodyParts.ContainsKey(Entry.Key))
			{
				InternalRootBodyParts[Entry.Key].UpdateChildren(Entry.Value);
			}
		}
	}

	public void UpdateCustomisations(string InOld, string InNew)
	{
		if (isServer) return;
		PlayerSpritesData = InNew;
		playerSprites.UpdateChildren(JsonConvert.DeserializeObject<List<RootBodyPartContainer.intName>>(PlayerSpritesData));
	}

}