using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public static class SpawnHandler
{
	private static readonly CustomNetworkManager networkManager = CustomNetworkManager.Instance;

	public static void SpawnViewer(NetworkConnection conn, short playerControllerId, JobType jobType = JobType.NULL)
	{
		GameObject joinedViewer = Object.Instantiate(networkManager.playerPrefab);
		NetworkServer.AddPlayerForConnection(conn, joinedViewer, 0);
	}

	public static void SpawnDummyPlayer(JobType jobType = JobType.NULL)
	{
		GameObject dummyPlayer = CreatePlayer(jobType);
		TransferPlayer(null, 0, dummyPlayer, null, EVENT.PlayerSpawned, null);
	}

	public static void ClonePlayer(NetworkConnection conn, short playerControllerId, JobType jobType, CharacterSettings characterSettings, GameObject oldBody, GameObject spawnSpot)
	{
		GameObject player = CreateMob(spawnSpot, CustomNetworkManager.Instance.humanPlayerPrefab);
		TransferPlayer(conn, playerControllerId, player, oldBody, EVENT.PlayerSpawned, characterSettings);
		var playerScript = player.GetComponent<PlayerScript>();
		var oldPlayerScript = oldBody.GetComponent<PlayerScript>();
		oldPlayerScript.mind.SetNewBody(playerScript);
	}

	public static void RespawnPlayer(NetworkConnection conn, short playerControllerId, JobType jobType, CharacterSettings characterSettings, GameObject oldBody)
	{
		GameObject player = CreatePlayer(jobType);
		TransferPlayer(conn, playerControllerId, player, oldBody, EVENT.PlayerSpawned, characterSettings);
		new Mind(player, jobType);
		var equipment = player.GetComponent<Equipment>();

		var playerScript = player.GetComponent<PlayerScript>();
		var connectedPlayer = PlayerList.Instance.Get(conn);
		connectedPlayer.Name = playerScript.playerName;
		UpdateConnectedPlayersMessage.Send();
		PlayerList.Instance.TryAddScores(playerScript.playerName);

		equipment.SetPlayerLoadOuts();
		if(jobType != JobType.SYNDICATE && jobType != JobType.AI)
		{
			SecurityRecordsManager.Instance.AddRecord(playerScript, jobType);
		}
	}

	public static GameObject SpawnPlayerGhost(NetworkConnection conn, short playerControllerId, GameObject oldBody, CharacterSettings characterSettings)
	{
		GameObject ghost = CreateMob(oldBody, CustomNetworkManager.Instance.ghostPrefab);
		TransferPlayer(conn, playerControllerId, ghost, oldBody, EVENT.GhostSpawned, characterSettings);
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
	public static void TransferPlayer(NetworkConnection conn, short playerControllerId, GameObject newBody, GameObject oldBody, EVENT eventType, CharacterSettings characterSettings)
	{
		var oldPlayerNetworkActions = oldBody.GetComponent<PlayerNetworkActions>();
		if(oldPlayerNetworkActions)
		{
			oldPlayerNetworkActions.RpcBeforeBodyTransfer();
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
			NetworkServer.ReplacePlayerForConnection(conn, newBody, playerControllerId);
			NetworkServer.ReplacePlayerForConnection(new NetworkConnection(), oldBody, 0);
			TriggerEventMessage.Send(newBody, eventType);
		}
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
		NetworkInstanceId parentNetId;
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
			parentNetId = matrixInfo.NetId;
			parentTransform = matrixInfo.Objects;
		}

		GameObject newMob = Object.Instantiate(mobPrefab, spawnPosition, Quaternion.identity, parentTransform);
		var playerScript = newMob.GetComponent<PlayerScript>();

		playerScript.registerTile.ParentNetId = parentNetId;

		return newMob;
	}

	private static GameObject CreatePlayer(JobType jobType)
	{
		GameObject playerPrefab = CustomNetworkManager.Instance.humanPlayerPrefab;

		Transform spawnTransform = GetSpawnForJob(jobType);

		GameObject player;

		if (spawnTransform != null)
		{
			player = CreateMob(spawnTransform.gameObject, playerPrefab );
		}
		else
		{
			player = Object.Instantiate(playerPrefab);
		}

		return player;
	}

	private static Transform GetSpawnForJob(JobType jobType)
	{
		if (jobType == JobType.NULL)
		{
			return null;
		}

		List<SpawnPoint> spawnPoints = networkManager.startPositions.Select(x => x.GetComponent<SpawnPoint>())
			.Where(x => x.JobRestrictions.Contains(jobType)).ToList();

		return spawnPoints.Count == 0 ? null : spawnPoints.PickRandom().transform;
	}


}