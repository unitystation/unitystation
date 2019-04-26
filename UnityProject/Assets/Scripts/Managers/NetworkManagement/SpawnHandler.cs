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
		var conn = new NetworkConnection();
		GameObject dummyPlayer = CreatePlayer(jobType);
		var connectedPlayer = PlayerList.Instance.Get(conn);
		PlayerList.Instance.UpdatePlayer(conn, dummyPlayer);
		NetworkServer.AddPlayerForConnection(conn, dummyPlayer, 0);
		if (connectedPlayer.Script.PlayerSync != null) {
			connectedPlayer.Script.PlayerSync.NotifyPlayers(true);
		}
	}

	public static void RespawnPlayer(NetworkConnection conn, short playerControllerId, JobType jobType, GameObject oldBody)
	{
		GameObject player = CreatePlayer(jobType);
		TransferPlayer(conn, playerControllerId, player, oldBody, EVENT.PlayerSpawned);
	}

	public static void SpawnPlayerGhost(NetworkConnection conn, short playerControllerId, GameObject oldBody)
	{
		GameObject ghost = CreateGhost(oldBody);
		TransferPlayer(conn, playerControllerId, ghost, oldBody, EVENT.GhostSpawned);
	}

	/// <summary>
	/// Connects a client to a character or redirects a logged out ConnectedPlayer.
	/// </summary>
	/// <param name="conn">The client's NetworkConnection. If logged out the playerlist will return an invalid connectedplayer</param>
	/// <param name="playerControllerId">ID of the client player to be transfered. If logged out it's empty.</param>
	/// <param name="newBody">The character gameobject to be transfered into.</param>
	/// <param name="oldBody">The old body of the character.</param>
	/// <param name="eventType">Event type for the player sync.</param>
	public static void TransferPlayer(NetworkConnection conn, short playerControllerId, GameObject newBody, GameObject oldBody, EVENT eventType)
	{
		var connectedPlayer = PlayerList.Instance.Get(conn);
		if (connectedPlayer == ConnectedPlayer.Invalid)
		{
			PlayerList.Instance.UpdateLoggedOffPlayer(newBody, oldBody);
			return;
		}
		else
		{
			PlayerList.Instance.UpdatePlayer(conn, newBody);
			NetworkServer.ReplacePlayerForConnection(conn, newBody, playerControllerId);
			TriggerEventMessage.Send(newBody, eventType);
		}
		var playerScript = newBody.GetComponent<PlayerScript>();
		if (playerScript.PlayerSync != null)
		{
			playerScript.PlayerSync.NotifyPlayers(true);
		}
	}

	private static GameObject CreateGhost(GameObject oldBody)
	{
		GameObject ghostPrefab = CustomNetworkManager.Instance.ghostPrefab;
		Vector3 spawnPosition = oldBody.GetComponent<ObjectBehaviour>().AssumedLocation();
		Transform parent = oldBody.GetComponentInParent<ObjectLayer>().transform;

		GameObject ghost = Object.Instantiate(ghostPrefab, spawnPosition, Quaternion.identity, parent);
		ghost.GetComponent<RegisterPlayer>().ParentNetId = oldBody.transform.parent.GetComponentInParent<NetworkIdentity>().netId;
		//they are a ghost but we still need to preserve job type so they can respawn with the correct job
		ghost.GetComponent<PlayerScript>().JobType = oldBody.GetComponent<PlayerScript>().JobType;

		return ghost;
	}

	private static GameObject CreatePlayer(JobType jobType)
	{
		GameObject playerPrefab = CustomNetworkManager.Instance.humanPlayerPrefab;

		Transform spawnPosition = GetSpawnForJob(jobType);

		GameObject player;

		if (spawnPosition != null)
		{
			Vector3 position = spawnPosition.position;
			Quaternion rotation = spawnPosition.rotation;
			Transform parent = spawnPosition.GetComponentInParent<ObjectLayer>().transform;
			player = Object.Instantiate(playerPrefab, position, rotation, parent);
			player.GetComponent<RegisterPlayer>().ParentNetId = spawnPosition.GetComponentInParent<NetworkIdentity>().netId;
		}
		else
		{
			player = Object.Instantiate(playerPrefab);
		}

		player.GetComponent<PlayerScript>().JobType = jobType;

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