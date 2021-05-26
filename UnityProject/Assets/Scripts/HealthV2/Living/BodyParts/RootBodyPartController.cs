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
	private string PlayerSpritesData = "";

	[SyncVar(hook = nameof(UpdateChildren))]
	private string SerialisedNetIDs = "";

	[System.Serializable]
	public struct StringClass
	{
		public string String;
		public RootBodyPartContainer BodyPart;
	}

	public List<RootBodyPartContainer> RootBodyParts = new List<RootBodyPartContainer>();

	private Dictionary<string, RootBodyPartContainer> InternalRootBodyParts =
		new Dictionary<string, RootBodyPartContainer>();

	public Dictionary<string, List<uint>> DictionaryToSerialised = new Dictionary<string, List<uint>>();

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
		DictionaryToSerialised[RootBodyPartContainer.name] =
			RootBodyPartContainer.InternalNetIDs;

		UpdateChildren("", JsonConvert.SerializeObject(DictionaryToSerialised));
	}


	public void UpdateChildren(string InOld, string InNew)
	{
		SerialisedNetIDs = InNew;
		if (CustomNetworkManager.Instance._isServer) return;
		StartCoroutine(WaitForStartChildren());
	}


	public void UpdateCustomisations(string InOld, string InNew)
	{
		PlayerSpritesData = InNew;
		if (CustomNetworkManager.Instance._isServer) return;
		StartCoroutine(WaitForStartCustomisations());
	}

	private IEnumerator WaitForStartChildren()
	{
		yield return null;
		yield return null;
		playerSprites.Addedbodypart.Clear();
		DictionaryToSerialised = JsonConvert.DeserializeObject<Dictionary<string, List<uint>>>(SerialisedNetIDs);
		foreach (var Entry in DictionaryToSerialised)
		{
			if (InternalRootBodyParts.ContainsKey(Entry.Key))
			{
				InternalRootBodyParts[Entry.Key].UpdateChildren(Entry.Value);
			}
		}
	}

	private IEnumerator WaitForStartCustomisations()
	{
		yield return null;
		yield return null;
		playerSprites.UpdateChildren(JsonConvert.DeserializeObject<List<uint>>(PlayerSpritesData));
	}
}