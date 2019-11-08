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

	//TODO: Not currently allowing dummy spawning
//	public static void SpawnDummyPlayer(Occupation occupation)
//	{
//		//TODO: Not sure this still works
//		GameObject playerPrefab = CustomNetworkManager.Instance.humanPlayerPrefab;
//
//		Transform spawnTransform = GetSpawnForJob(occupation.JobType);
//
//		GameObject dummy;
//
//		if (spawnTransform != null)
//		{
//			dummy = CreateMob(spawnTransform.gameObject, playerPrefab );
//		}
//		else
//		{
//			dummy = Object.Instantiate(playerPrefab);
//		}
//		TransferPlayer(null, dummy, null, EVENT.PlayerSpawned, null, occupation);
//	}

	public static void ClonePlayer(NetworkConnection conn, Occupation occupation, CharacterSettings characterSettings, GameObject oldBody, GameObject spawnSpot)
	{
		GameObject player = CreatePlayer(occupation, characterSettings);
		TransferPlayer(conn, player, oldBody, EVENT.PlayerSpawned, characterSettings, occupation);
		var playerScript = player.GetComponent<PlayerScript>();
		var oldPlayerScript = oldBody.GetComponent<PlayerScript>();
		oldPlayerScript.mind.SetNewBody(playerScript);
	}

	public static void RespawnPlayer(NetworkConnection conn, Occupation occupation, CharacterSettings characterSettings, GameObject oldBody)
	{
		GameObject newPlayer = CreatePlayer(occupation, characterSettings);
		TransferPlayer(conn, newPlayer, oldBody, EVENT.PlayerSpawned, characterSettings, occupation);
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

	/// <summary>
	/// Connects a client to a character or redirects a logged out ConnectedPlayer.
	/// </summary>
	/// <param name="conn">The client's NetworkConnection. If logged out the playerlist will return an invalid connectedplayer</param>
	/// <param name="playerControllerId">ID of the client player to be transfered. If logged out it's empty.</param>
	/// <param name="newBody">The character gameobject to be transfered into.</param>
	/// <param name="oldBody">The old body of the character.</param>
	/// <param name="eventType">Event type for the player sync.</param>
	/// <param name="characterSettings">settings, ignored if transferring to an existing player body</param>
	/// <param name="occupation">occupation, ignored if transferring to an existing player body</param>
	public static void TransferPlayer(NetworkConnection conn, GameObject newBody, GameObject oldBody, EVENT eventType, CharacterSettings characterSettings, Occupation occupation)
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
			Spawn.ServerCreatePlayer(occupation, characterSettings, newBody);
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

	public static GameObject SpawnPlayerGhost(NetworkConnection conn, GameObject oldBody, CharacterSettings characterSettings, Occupation occupation)
	{
		//determine where to spawn the ghost
		var registerTile = oldBody.GetComponent<RegisterTile>();
		if (registerTile == null)
		{
			Logger.LogErrorFormat("Cannot spawn ghost for body {0} because it has no registerTile", Category.ItemSpawn,
				oldBody.name);
			return null;
		}

		Vector3Int spawnPosition = oldBody.GetComponent<ObjectBehaviour>().AssumedWorldPositionServer().RoundToInt();
		var ghost = Spawn.ServerCreateGhost(occupation, characterSettings, CustomNetworkManager.Instance.ghostPrefab,
			spawnPosition).GameObject;
		TransferPlayer(conn, ghost, oldBody, EVENT.GhostSpawned, characterSettings, occupation);
		return ghost;
	}

	private static GameObject CreatePlayer(Occupation occupation, CharacterSettings settings)
	{
		//determine where to spawn them
		Transform spawnTransform = GetSpawnForJob(occupation.JobType);
		if (spawnTransform == null)
		{
			Logger.LogError("Unable to determine spawn position. Cannot create player.", Category.ItemSpawn);
			return null;
		}

		return Spawn.ServerCreatePlayer(occupation, settings, CustomNetworkManager.Instance.humanPlayerPrefab,
			spawnTransform.transform.position.CutToInt()).GameObject;
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