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

	public static void RespawnPlayer(NetworkConnection conn, short playerControllerId, JobType jobType)
	{
		GameObject player = CreatePlayer(jobType);
		var connectedPlayer = PlayerList.Instance.Get(conn);
		PlayerList.Instance.UpdatePlayer(conn, player);
		NetworkServer.ReplacePlayerForConnection(conn, player, playerControllerId);
		TriggerEventMessage.Send(player, EVENT.PlayerSpawned);
		if (connectedPlayer.Script.PlayerSync != null) {
			connectedPlayer.Script.PlayerSync.NotifyPlayers(true);
		}
	}

	/// <summary>
	/// Spawn the ghost as a new object and set that as the focus / what the user controls. Leave the player body where it is.
	/// </summary>
	/// <param name="conn">connection whose ghost is being spawned</param>
	/// <param name="playerControllerId">ID of player whose ghost should be spawned</param>
	/// <returns>the gameobject of the ghost</returns>
	public static void SpawnPlayerGhost(NetworkConnection conn, short playerControllerId)
	{
		var connectedPlayer = PlayerList.Instance.Get(conn);
		GameObject body = connectedPlayer.GameObject;
		JobType savedJobType = body.GetComponent<PlayerScript>().JobType;
		GameObject ghost = CreateGhost(connectedPlayer, savedJobType);
		PlayerList.Instance.UpdatePlayer(conn, ghost);
		NetworkServer.ReplacePlayerForConnection(conn, ghost, playerControllerId);
		TriggerEventMessage.Send(ghost, EVENT.GhostSpawned);
		if (connectedPlayer.Script.PlayerSync != null) {
			connectedPlayer.Script.PlayerSync.NotifyPlayers(true);
		}
	}

	private static GameObject CreateGhost(ConnectedPlayer forPlayer, JobType jobType)
	{
		GameObject ghostPrefab = CustomNetworkManager.Instance.ghostPrefab;

		Vector3 spawnPosition = forPlayer.GameObject.GetComponent<ObjectBehaviour>().AssumedLocation();
		GameObject body = forPlayer.GameObject;

		GameObject ghost;

		Transform parent = body.GetComponentInParent<ObjectLayer>().transform;
		ghost = Object.Instantiate(ghostPrefab, spawnPosition, Quaternion.identity, parent);
		ghost.GetComponent<RegisterPlayer>().ParentNetId = body.transform.parent.GetComponentInParent<NetworkIdentity>().netId;


		//they are a ghost but we still need to preserve job type so they can respawn with the correct job
		ghost.GetComponent<PlayerScript>().JobType = jobType;

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