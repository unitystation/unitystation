using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;


/// <summary>
/// Connects two portals together, can be separate from the station gateway.
/// </summary>
public class WorldGateway : StationGateway
{
	[SerializeField]
	private GameObject StationGateway = null;// doesnt have to be station just the gateway this one will connect to

	public override void OnStartServer()
	{
		SetOffline();
		registerTile = GetComponent<RegisterTile>();
		Position = registerTile.WorldPosition;
		SubSceneManager.RegisterWorldGateway(this);
		ServerChangeState(false);
	}

	[Server]
	public void SetUp(StationGateway stationGateway)
	{
		StationGateway = stationGateway.gameObject;
		Message = "Teleporting to: " + stationGateway.WorldName;

		SetOnline();
		ServerChangeState(true);
		if (GetComponent<MobSpawnControlScript>() != null)
		{
			GetComponent<MobSpawnControlScript>().SpawnMobs();
		}

		SpawnedMobs = true;
	}

	[Server]
	public override void TransportPlayers(ObjectBehaviour player)
	{
		//teleports player to the front of the new gateway
		player.GetComponent<PlayerSync>().SetPosition(StationGateway.GetComponent<RegisterTile>().WorldPosition);
	}

	[Server]
	public override void TransportObjectsItems(ObjectBehaviour objectsitems)
	{
		objectsitems.GetComponent<CustomNetTransform>().SetPosition(StationGateway.GetComponent<RegisterTile>().WorldPosition);
	}
}
