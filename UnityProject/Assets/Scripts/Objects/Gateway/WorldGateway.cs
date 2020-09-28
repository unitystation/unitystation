using System;
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
	private GameObject StationGateway = null; // doesnt have to be station just the gateway this one will connect to

	/// <summary>
	/// Should the mobs spawn when gate connects
	/// </summary>
	public bool spawnMobsOnConnection;

	/// <summary>
	/// For world gate to world gate
	/// </summary>
	[SerializeField]
	private bool ifWorldGateToWorldGate = default;

	/// <summary>
	/// If you want a person traveling to this gate to go somewhere else. WORLD POS, not local
	/// </summary>
	public Vector3Int  OverrideCoord;

	/// <summary>
	/// Gets the coordinates of the teleport target where things will be teleported to.
	/// </summary>
	public override Vector3 TeleportTargetCoord => StationGateway.GetComponent<RegisterTile>().WorldPosition;

	public override void OnStartServer()
	{
		SetOffline();
		registerTile = GetComponent<RegisterTile>();

		ServerChangeState(false);

		if (ifWorldGateToWorldGate && StationGateway != null)
		{
			SetUpWorldToWorld();
		}
		else if (!ifWorldGateToWorldGate)
		{
			SubSceneManager.RegisterWorldGateway(this);
		}
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
	public void SetUpWorldToWorld()
	{
		Message = "Teleporting...";

		SetOnline();
		ServerChangeState(true);

		if (GetComponent<MobSpawnControlScript>() != null)
		{
			GetComponent<MobSpawnControlScript>().SpawnMobs();
		}

		SpawnedMobs = true;
	}
}
