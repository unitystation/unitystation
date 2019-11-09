using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

/// <summary>
/// Main API for dealing with spawning players and related things.
/// For spawning of non-player things, see Spawn.
/// </summary>
public static class PlayerSpawn
{
	/// <summary>
	/// Server-side only. For use when a player has only joined (as a JoinedViewer) and
	/// is not in control of any mobs. Spawns the player with the specified occupation / settings
	/// and transfers the viewer to control the new player.
	/// </summary>
	/// <param name="forViewer">viewer who should control the player</param>
	/// <param name="occupation">occupation to spawn as</param>
	/// <param name="characterSettings">settings to use for the character</param>
	/// <returns></returns>
	public static void ServerSpawnPlayer(JoinedViewer forViewer, Occupation occupation, CharacterSettings characterSettings)
	{
		NetworkConnection conn = forViewer.connectionToClient;
		ServerSpawnInternal(conn, occupation, characterSettings, null);
	}

	/// <summary>
	/// For use when player is connected and dead.
	/// Respawns the mind's character and transfers their control to it.
	/// </summary>
	/// <param name="forMind"></param>
	public static void ServerRespawnPlayer(Mind forMind)
	{
		//get the settings from the mind
		var occupation = forMind.occupation;
		var oldBody = forMind.GetCurrentMob();
		var connection = oldBody.GetComponent<NetworkIdentity>().connectionToClient;
		var settings = oldBody.GetComponent<PlayerScript>().characterSettings;
		var oldGhost = forMind.ghost;

		ServerSpawnInternal(connection, occupation, settings, forMind);

		if (oldGhost)
		{
			Despawn.ServerSingle(oldGhost.gameObject);
		}

	}

	/// <summary>
	/// For use when a player is alive or dead and you want to clone their body and transfer their control to it.
	///
	/// Clones a given mind's current body to a new body and transfers control of the mind's connection to that new body.
	/// </summary>
	/// <param name="forMind"></param>
	/// <param name="worldPosition"></param>
	public static void ServerClonePlayer(Mind forMind, Vector3Int worldPosition)
	{
		//TODO: Can probably remove characterSettings from cloningrecord
		//determine previous occupation / settings
		var oldBody = forMind.GetCurrentMob();
		var occupation = forMind.occupation;
		var connection = oldBody.GetComponent<NetworkIdentity>().connectionToClient;
		var settings = oldBody.GetComponent<PlayerScript>().characterSettings;

		ServerSpawnInternal(connection, occupation, settings, forMind, worldPosition, true);
	}

	/// <summary>
	/// Spawns a new player character and transfers the connection's control into the new body.
	/// If existingMind is null, creates the new mind and assigns it to the new body.
	///
	/// Fires server and client side player spawn hooks.
	/// </summary>
	/// <param name="connection">connection to give control to the new player character</param>
	/// <param name="occupation">occupation of the new player character</param>
	/// <param name="characterSettings">settings of the new player character</param>
	/// <param name="existingMind">existing mind to transfer to the new player, if null new mind will be created
	/// and assigned to the new player character</param>
	/// <param name="spawnPos"></param>
	private static void ServerSpawnInternal(NetworkConnection connection, Occupation occupation, CharacterSettings characterSettings,
		Mind existingMind, Vector3Int? spawnPos = null, bool naked = false)
	{
		//determine where to spawn them
		if (spawnPos == null)
		{
			Transform spawnTransform = GetSpawnForJob(occupation.JobType);
			if (spawnTransform == null)
			{
				Logger.LogErrorFormat(
					"Unable to determine spawn position for connection {0} occupation {1}. Cannot spawn player.",
					Category.ItemSpawn,
					connection.address, occupation.DisplayName);
				return;
			}

			spawnPos = spawnTransform.transform.position.CutToInt();
		}

		//create the player object
		var newPlayer = ServerCreatePlayer(spawnPos.GetValueOrDefault(),
			occupation, characterSettings);
		var newPlayerScript = newPlayer.GetComponent<PlayerScript>();

		//get the old body if they have one.
		var oldBody = existingMind?.GetCurrentMob();

		//transfer control to the player object
		ServerTransferPlayer(connection, newPlayer, oldBody, EVENT.PlayerSpawned, characterSettings, occupation);


		if (existingMind == null)
		{
			//create the mind of the player
			Mind.Create(newPlayer, occupation);
		}
		else
		{
			//transfer the mind to the new body
			existingMind.SetNewBody(newPlayerScript);
		}


		var ps = newPlayer.GetComponent<PlayerScript>();
		var connectedPlayer = PlayerList.Instance.Get(connection);
		connectedPlayer.Name = ps.playerName;
		UpdateConnectedPlayersMessage.Send();
		PlayerList.Instance.TryAddScores(ps.playerName);

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

		if (occupation.JobType != JobType.SYNDICATE && occupation.JobType != JobType.AI)
		{
			SecurityRecordsManager.Instance.AddRecord(ps, occupation.JobType);
		}

		//fire all hooks
		var info = SpawnInfo.Player(occupation, characterSettings, CustomNetworkManager.Instance.humanPlayerPrefab,
			spawnPos, naked: naked);
		Spawn._ServerFireClientServerSpawnHooks(SpawnResult.Single(info, newPlayer));
	}

	/// <summary>
	/// Use this when a player is currently a ghost and wants to reenter their body.
	/// </summary>
	/// <param name="forConnection">connection to transfer control to</param>
	/// TODO: Remove need for this parameter
	/// <param name="forConnection">object forConnection is currently in control of</param>
	/// <param name="forMind">mind to transfer control back into their body</param>
	public static void ServerGhostReenterBody(NetworkConnection forConnection, GameObject fromObject, Mind forMind)
	{
		var body = forMind.GetCurrentMob();
		var oldGhost = forMind.ghost;
		var ps = body.GetComponent<PlayerScript>();
		var mind = ps.mind;
		var occupation = mind.occupation;
		var settings = ps.characterSettings;
		ServerTransferPlayer(forConnection, body, fromObject, EVENT.PlayerRejoined, settings, occupation);
		body.GetComponent<PlayerScript>().playerNetworkActions.ReenterBodyUpdates();

		if (oldGhost)
		{
			Despawn.ServerSingle(oldGhost.gameObject);
		}
	}


	/// <summary>
	/// Use this when a player rejoins the game and already has a logged-out body in the game.
	/// Transfers their control back to the body.
	/// </summary>
	/// <param name="viewer"></param>
	/// <param name="body">to transfer into</param>
	public static void ServerRejoinPlayer(JoinedViewer viewer, GameObject body)
	{
		var ps = body.GetComponent<PlayerScript>();
		var mind = ps.mind;
		var occupation = mind.occupation;
		var settings = ps.characterSettings;
		ServerTransferPlayer(viewer.connectionToClient, body, viewer.gameObject, EVENT.PlayerRejoined, settings, occupation);
		body.GetComponent<PlayerScript>().playerNetworkActions.ReenterBodyUpdates();
	}

	/// <summary>
	/// Spawns a ghost for the indicated mind's body and transfers the connection's control to it.
	/// </summary>
	/// <param name="conn"></param>
	/// <param name="oldBody"></param>
	/// <param name="characterSettings"></param>
	/// <param name="occupation"></param>
	/// <returns></returns>
	public static void ServerSpawnGhost(Mind forMind)
	{
		//determine where to spawn the ghost
		var body = forMind.GetCurrentMob();
		var settings = body.GetComponent<PlayerScript>().characterSettings;
		var connection = body.GetComponent<NetworkIdentity>().connectionToClient;
		var registerTile = body.GetComponent<RegisterTile>();
		if (registerTile == null)
		{
			Logger.LogErrorFormat("Cannot spawn ghost for body {0} because it has no registerTile", Category.ItemSpawn,
				body.name);
			return;
		}

		Vector3Int spawnPosition = body.GetComponent<ObjectBehaviour>().AssumedWorldPositionServer().RoundToInt();
		if (spawnPosition == TransformState.HiddenPos)
		{
			//spawn ghost at occupation location if we can't determine where their body is
			Transform spawnTransform = GetSpawnForJob(forMind.occupation.JobType);
			if (spawnTransform == null)
			{
				Logger.LogErrorFormat("Unable to determine spawn position for occupation {1}. Cannot spawn ghost.", Category.ItemSpawn,
					forMind.occupation.DisplayName);
				return;
			}

			spawnPosition = spawnTransform.transform.position.CutToInt();
		}

		var matrixInfo = MatrixManager.AtPoint( spawnPosition, true );
		var parentNetId = matrixInfo.NetID;
		var parentTransform = matrixInfo.Objects;

		var ghost = Object.Instantiate(CustomNetworkManager.Instance.ghostPrefab, spawnPosition, Quaternion.identity,
			parentTransform);
		ghost.GetComponent<PlayerScript>().registerTile.ParentNetId = parentNetId;

		forMind.Ghosting(ghost);

		ServerTransferPlayer(connection, ghost, body, EVENT.GhostSpawned, settings, forMind.occupation);


		//fire all hooks
		var info = SpawnInfo.Ghost(forMind.occupation, settings, CustomNetworkManager.Instance.ghostPrefab, spawnPosition,
			parentTransform);
		Spawn._ServerFireClientServerSpawnHooks(SpawnResult.Single(info, ghost));
	}

	/// <summary>
	/// Server-side only. Creates the player object on the server side and fires server-side
	/// spawn hooks. Doesn't transfer control to the client yet. Client side hooks should be fired after client has been
	/// informed of the spawn
	/// </summary>
	/// <param name="spawnWorldPosition">world pos to spawn at</param>
	/// <param name="occupation">occupation to spawn as</param>
	/// <param name="characterSettings">settings to use for the character</param>
	/// <returns></returns>
	private static GameObject ServerCreatePlayer(Vector3Int spawnWorldPosition, Occupation occupation, CharacterSettings settings)
	{
		//player is only spawned on server, we don't sync it to other players yet
		var spawnPosition = spawnWorldPosition;
		var matrixInfo = MatrixManager.AtPoint( spawnPosition, true );
		var parentNetId = matrixInfo.NetID;
		var parentTransform = matrixInfo.Objects;

		var player = Object.Instantiate(CustomNetworkManager.Instance.humanPlayerPrefab,
			spawnPosition, Quaternion.identity,
			parentTransform);
		player.GetComponent<PlayerScript>().registerTile.ParentNetId = parentNetId;



		return player;
	}

	/// <summary>
	/// Server-side only. Transfers control of a player object to the indicated connection.
	/// </summary>
	/// <param name="conn">The client's NetworkConnection. If logged out the playerlist will return an invalid connectedplayer</param>
	/// <param name="newBody">The character gameobject to be transfered into.</param>
	/// <param name="oldBody">The old body of the character.</param>
	/// <param name="eventType">Event type for the player sync.</param>
	/// <param name="characterSettings">settings, ignored if transferring to an existing player body</param>
	/// <param name="occupation">occupation, ignored if transferring to an existing player body</param>
	private static void ServerTransferPlayer(NetworkConnection conn, GameObject newBody, GameObject oldBody,
		EVENT eventType, CharacterSettings characterSettings, Occupation occupation)
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
			Logger.LogErrorFormat("Cannot transfer player from {0} to {1}, networkconnection of player to transfer is not online.",
				Category.ItemSpawn, oldBody, newBody);
			return;
		}

		PlayerList.Instance.UpdatePlayer(conn, newBody);
		NetworkServer.ReplacePlayerForConnection(conn, newBody);
		if (oldBody)
		{
			NetworkServer.ReplacePlayerForConnection(new NetworkConnection("0.0.0.0"), oldBody);
		}
		TriggerEventMessage.Send(newBody, eventType);
		//can observe their new inventory
		newBody.GetComponent<ItemStorage>()?.ServerAddObserverPlayer(newBody);

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


	/// <summary>
	/// Spawns a viewer for the specified connection and transfer the connection to this viewer.
	/// </summary>
	/// <param name="conn"></param>
	public static void ServerSpawnViewer(NetworkConnection conn)
	{
		GameObject joinedViewer = Object.Instantiate(CustomNetworkManager.Instance.playerPrefab);
		NetworkServer.AddPlayerForConnection(conn, joinedViewer, System.Guid.NewGuid());
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



}