using System;
using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

//TODO REMOVE ALL OF THIS USING A FLAMETHROWER
public class RootBodyPartController : NetworkBehaviour
{
	[SyncVar(hook = nameof(UpdateCustomisations))]
	public string PlayerSpritesData = "";

	[SyncVar(hook = nameof(UpdateChildren))]
	private string SerialisedNetIDs = "";

	public List<uint> ToSerialised = new List<uint>();

	private LivingHealthMasterBase livingHealth;

	private void Awake()
	{
		livingHealth = GetComponent<LivingHealthMasterBase>();
	}

	public void RequestUpdate()
	{
		ToSerialised = livingHealth.InternalNetIDs;
		SerialisedNetIDs = JsonConvert.SerializeObject(ToSerialised);
	}

	public void UpdateChildren(string InOld, string InNew)
	{
		SerialisedNetIDs = InNew;
		livingHealth.playerSprites.Addedbodypart.Clear();
		ToSerialised = JsonConvert.DeserializeObject<List<uint>>(SerialisedNetIDs);
		livingHealth.UpdateChildren(ToSerialised);
	}

	public void UpdateCustomisations(string InOld, string InNew)
	{
		PlayerSpritesData = InNew;
		livingHealth.playerSprites.UpdateChildren(JsonConvert.DeserializeObject<List<uint>>(PlayerSpritesData));
	}

}