using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

public static class PlayerSpawnHandler
{
	private static readonly CustomNetworkManager networkManager = CustomNetworkManager.Instance;

	public static void SpawnViewer(NetworkConnection conn)
	{
		GameObject joinedViewer = Object.Instantiate(networkManager.playerPrefab);
		NetworkServer.AddPlayerForConnection(conn, joinedViewer, System.Guid.NewGuid());
	}

	//TODO: Temporarily removing dummy spawning capability while I figure out spawning
//	public static void SpawnDummyPlayer(Occupation occupation)
//	{
//		GameObject dummyPlayer = CreatePlayer(occupation);
//		TransferPlayer(null, dummyPlayer, null, EVENT.PlayerSpawned, null);
//	}

	public static void ClonePlayer(NetworkConnection conn, Occupation occupation, CharacterSettings characterSettings, GameObject oldBody, GameObject spawnSpot)
	{
		GameObject player = CreatePlayer(occupation, characterSettings);
		TransferPlayer(conn, player, oldBody, EVENT.PlayerSpawned, characterSettings);
		var playerScript = player.GetComponent<PlayerScript>();
		var oldPlayerScript = oldBody.GetComponent<PlayerScript>();
		oldPlayerScript.mind.SetNewBody(playerScript);
	}

	public static void RespawnPlayer(NetworkConnection conn, Occupation occupation, CharacterSettings characterSettings, GameObject oldBody)
	{
		GameObject newPlayer = CreatePlayer(occupation, characterSettings);
		TransferPlayer(conn, newPlayer, oldBody, EVENT.PlayerSpawned, characterSettings);
		new Mind(newPlayer, occupation);
		var equipment = newPlayer.GetComponent<Equipment>();

		var playerScript = newPlayer.GetComponent<PlayerScript>();
		var connectedPlayer = PlayerList.Instance.Get(conn);
		connectedPlayer.Name = playerScript.playerName;
		UpdateConnectedPlayersMessage.Send();
		PlayerList.Instance.TryAddScores(playerScript.playerName);

		if (occupation.JobType == JobType.SYNDICATE)
		{
			//Check to see if there is a nuke and communicate the nuke code:
			Nuke nuke = Object.FindObjectOfType<Nuke>();
			if (nuke != null)
			{
				UpdateChatMessage.Send(newPlayer, ChatChannel.Syndicate, ChatModifier.None,
					"We have intercepted the code for the nuclear weapon: " + nuke.NukeCode);
			}
		}

		if(occupation.JobType != JobType.SYNDICATE && occupation.JobType != JobType.AI)
		{
			SecurityRecordsManager.Instance.AddRecord(playerScript, occupation.JobType);
		}
	}

	public static GameObject SpawnPlayerGhost(NetworkConnection conn, GameObject oldBody, CharacterSettings characterSettings)
	{
		GameObject ghost = CreateMob(oldBody, CustomNetworkManager.Instance.ghostPrefab);
		TransferPlayer(conn, ghost, oldBody, EVENT.GhostSpawned, characterSettings);
		return ghost;
	}

	/// <summary>
	/// Connects a client to a character or redirects a logged out ConnectedPlayer.
	/// </summary>
	/// <param name="conn">The client's NetworkConnection. If logged out the playerlist will return an invalid connectedplayer</param>
	/// <param name="playerControllerId">ID of the client player to be transfered. If logged out it's empty.</param>
	/// <param name="newBody">The character gameobject to be transfered into.</param>
	/// <param name="oldBody">The old body of the character.</param>
	/// <param name="eventType">Event type for the player sync.</param>
	public static void TransferPlayer(NetworkConnection conn, GameObject newBody, GameObject oldBody, EVENT eventType, CharacterSettings characterSettings)
	{
		if (oldBody)
		{
			var oldPlayerNetworkActions = oldBody.GetComponent<PlayerNetworkActions>();
			if (oldPlayerNetworkActions)
			{
				oldPlayerNetworkActions.RpcBeforeBodyTransfer();
			}
			//no longer can observe their inventory
			oldBody.GetComponent<ItemStorage>()?.ServerRemoveObserverPlayer(oldBody);
		}

		var connectedPlayer = PlayerList.Instance.Get(conn);
		if (connectedPlayer == ConnectedPlayer.Invalid) //this isn't an online player
		{
			PlayerList.Instance.UpdateLoggedOffPlayer(newBody, oldBody);
			NetworkServer.Spawn(newBody);
		}
		else
		{
			PlayerList.Instance.UpdatePlayer(conn, newBody);
			NetworkServer.ReplacePlayerForConnection(conn, newBody);
			NetworkServer.ReplacePlayerForConnection(new NetworkConnection("0.0.0.0"), oldBody);
			TriggerEventMessage.Send(newBody, eventType);
		}

		//can observe their new inventory
		newBody.GetComponent<ItemStorage>().ServerAddObserverPlayer(newBody);

		var playerScript = newBody.GetComponent<PlayerScript>();
		if (playerScript.PlayerSync != null)
		{
			playerScript.PlayerSync.NotifyPlayers(true);
		}

		// If the player is inside a container, send a ClosetHandlerMessage.
		// The ClosetHandlerMessage will attach the container to the transfered player.
		var playerObjectBehavior = newBody.GetComponent<ObjectBehaviour>();
		if (playerObjectBehavior && playerObjectBehavior.parentContainer)
		{
			ClosetHandlerMessage.Send(newBody, playerObjectBehavior.parentContainer.gameObject);
		}
		bool newMob = false;
		if(characterSettings != null)
		{
			playerScript.characterSettings = characterSettings;
			playerScript.playerName = characterSettings.Name;
			newBody.name = characterSettings.Name;
			var playerSprites = newBody.GetComponent<PlayerSprites>();
			if (playerSprites)
			{
				playerSprites.OnCharacterSettingsChange(characterSettings);
			}
			newMob = true;
		}
		var healthStateMonitor = newBody.GetComponent<HealthStateMonitor>();
		if(healthStateMonitor)
		{
			healthStateMonitor.ProcessClientUpdateRequest(newBody);
		}
	}



	private static GameObject CreateMob(GameObject spawnSpot, GameObject mobPrefab)
	{
		var registerTile = spawnSpot.GetComponent<RegisterTile>();

		Transform parentTransform;
		uint parentNetId;
		Vector3Int spawnPosition;

		if ( registerTile ) //spawnSpot is someone's corpse
		{
			var objectLayer = registerTile.layer;
			parentNetId = objectLayer.GetComponentInParent<NetworkIdentity>().netId;
			parentTransform = objectLayer.transform;
			spawnPosition = spawnSpot.GetComponent<ObjectBehaviour>().AssumedWorldPositionServer().RoundToInt();
		}
		else //spawnSpot is a Spawnpoint object
		{
			spawnPosition = spawnSpot.transform.position.CutToInt();

			var matrixInfo = MatrixManager.AtPoint( spawnPosition, true );
			parentNetId = matrixInfo.NetID;
			parentTransform = matrixInfo.Objects;
		}

		GameObject newMob = Object.Instantiate(mobPrefab, spawnPosition, Quaternion.identity, parentTransform);
		var playerScript = newMob.GetComponent<PlayerScript>();

		playerScript.registerTile.ParentNetId = parentNetId;

		return newMob;
	}

	private static GameObject CreatePlayer(Occupation occupation, CharacterSettings settings)
	{
		var spawnTransform = GetSpawnForJob(occupation.JobType);
		if (spawnTransform == null)
		{
			return Spawn.ServerPlayer(occupation, settings, CustomNetworkManager.Instance.humanPlayerPrefab).GameObject;
		}

		var spawnSpot = spawnTransform.gameObject;
		var registerTile = spawnSpot.GetComponent<RegisterTile>();
		Transform parentTransform;
		uint parentNetId;
		Vector3Int spawnPosition;

		if ( registerTile ) //spawnSpot is someone's corpse
		{
			var objectLayer = registerTile.layer;
			parentNetId = objectLayer.GetComponentInParent<NetworkIdentity>().netId;
			parentTransform = objectLayer.transform;
			spawnPosition = spawnSpot.GetComponent<ObjectBehaviour>().AssumedWorldPositionServer().RoundToInt();
		}
		else //spawnSpot is a Spawnpoint object
		{
			spawnPosition = spawnSpot.transform.position.CutToInt();

			var matrixInfo = MatrixManager.AtPoint( spawnPosition, true );
			parentNetId = matrixInfo.NetID;
			parentTransform = matrixInfo.Objects;
		}

		var newPlayer = Spawn.ServerPlayer(occupation, settings, CustomNetworkManager.Instance.humanPlayerPrefab, spawnPosition,
			parentTransform).GameObject;
		var playerScript = newPlayer.GetComponent<PlayerScript>();

		playerScript.registerTile.ParentNetId = parentNetId;
		return newPlayer;

	}

	private static Transform GetSpawnForJob(JobType jobType)
	{
		if (jobType == JobType.NULL)
		{
			return null;
		}

		List<SpawnPoint> spawnPoints = CustomNetworkManager.startPositions.Select(x => x.GetComponent<SpawnPoint>())
			.Where(x => x.JobRestrictions.Contains(jobType)).ToList();

		return spawnPoints.Count == 0 ? null : spawnPoints.PickRandom().transform;
	}


}