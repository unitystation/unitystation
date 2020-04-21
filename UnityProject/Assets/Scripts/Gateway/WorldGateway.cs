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

	public bool IsOnlineAtStart = false;


	private void Start()
	{
		if (StationGateway == null) return;

		SetOffline();

		if (!isServer) return;

		SpawnedMobs = true;

		ServerChangeState(false);

		registerTile = GetComponent<RegisterTile>();

		Position = registerTile.WorldPosition;
		Message = "Teleporting to: " + StationGateway.GetComponent<StationGateway>().WorldName;

		if (IsOnlineAtStart && StationGateway != null)
		{
			SetOnline();
			ServerChangeState(true);

			if (GetComponent<MobSpawnControlScript>() != null)
			{
				GetComponent<MobSpawnControlScript>().SpawnMobs();
			}
		}
	}

	[Server]
	public void SetUp()
	{
		if (IsOnlineAtStart && StationGateway != null)
		{
			SetOnline();
			ServerChangeState(true);
		}
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
