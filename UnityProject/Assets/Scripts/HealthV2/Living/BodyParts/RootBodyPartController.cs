using System;
using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;
using Systems.Clothing;

//TODO REMOVE ALL OF THIS USING A FLAMETHROWER
public class RootBodyPartController : NetworkBehaviour
{
	[SyncVar(hook = nameof(SyncCustomizations))]
	public string PlayerSpritesData = "";

	[SyncVar(hook = nameof(SyncBodyParts))]
	private string SerialisedPrefabIDs = "";

	public List<IntName> ToSerialised = new List<IntName>();

	private LivingHealthMasterBase livingHealth;

	private void Awake()
	{
		livingHealth = GetComponent<LivingHealthMasterBase>();
	}

	public void UpdateClients()
	{
		ToSerialised = livingHealth.InternalNetIDs;
		SerialisedPrefabIDs = JsonConvert.SerializeObject(ToSerialised);
	}

	public void SyncBodyParts(string InOld, string InNew)
	{
		if (isServer)
			return;
		SerialisedPrefabIDs = InNew;
		ToSerialised = JsonConvert.DeserializeObject<List<IntName>>(SerialisedPrefabIDs);
		livingHealth.ClientUpdateSprites(ToSerialised);
	}

	public void SyncCustomizations(string InOld, string InNew)
	{
		if (isServer)
			return;
		PlayerSpritesData = InNew;
		livingHealth.playerSprites.UpdateChildren(JsonConvert.DeserializeObject<List<IntName>>(PlayerSpritesData));
	}

}

public class IntName
{
	public int Int;
	public string Name;

	[NonSerialized] public BodyPartSprites RelatedSprite;

	public ClothingHideFlags ClothingHide;
	public string Data;
}
