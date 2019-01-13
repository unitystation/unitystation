using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public static class SpawnHandler
{
	private static readonly CustomNetworkManager networkManager = CustomNetworkManager.Instance;

	public static void SpawnPlayer(NetworkConnection conn, short playerControllerId, JobType jobType = JobType.NULL)
	{
		GameObject player = CreatePlayer(jobType);
		var connectedPlayer = PlayerList.Instance.UpdatePlayer(conn, player);
		NetworkServer.AddPlayerForConnection(conn, player, playerControllerId);
		if (connectedPlayer.Script.PlayerSync != null) {
			connectedPlayer.Script.PlayerSync.NotifyPlayers(true);
		}
	}
	public static void SpawnDummyPlayer(JobType jobType = JobType.NULL)
	{
		var conn = new NetworkConnection();
		GameObject player = CreatePlayer(jobType);
		var connectedPlayer = PlayerList.Instance.UpdatePlayer(conn, player);
		NetworkServer.AddPlayerForConnection(conn, player, 0);
		if (connectedPlayer.Script.PlayerSync != null) {
			connectedPlayer.Script.PlayerSync.NotifyPlayers(true);
		}
	}

	public static void RespawnPlayer(NetworkConnection conn, short playerControllerId, JobType jobType)
	{
		GameObject player = CreatePlayer(jobType);
		var connectedPlayer = PlayerList.Instance.UpdatePlayer(conn, player);
		NetworkServer.ReplacePlayerForConnection(conn, player, playerControllerId);
		if (connectedPlayer.Script.PlayerSync != null) {
			connectedPlayer.Script.PlayerSync.NotifyPlayers(true);
		}
	}

	private static GameObject CreatePlayer(JobType jobType)
	{
		GameObject playerPrefab = networkManager.playerPrefab;

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