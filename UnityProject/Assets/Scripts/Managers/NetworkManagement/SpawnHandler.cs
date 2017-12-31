using System.Collections.Generic;
using System.Linq;
using PlayGroup;
using Tilemaps.Behaviours.Objects;
using Tilemaps.Scripts;
using Tilemaps.Scripts.Behaviours.Layers;
using UnityEngine;
using UnityEngine.Networking;

public static class SpawnHandler
{
	private static readonly CustomNetworkManager networkManager = CustomNetworkManager.Instance;

	public static void SpawnPlayer(NetworkConnection conn, short playerControllerId, JobType jobType = JobType.NULL)
	{
		GameObject player = CreatePlayer(jobType);
		NetworkServer.AddPlayerForConnection(conn, player, playerControllerId);
		
		UpdatePlayerList();
	}

	public static void RespawnPlayer(NetworkConnection conn, short playerControllerId, JobType jobType)
	{
		GameObject player = CreatePlayer(jobType);
		NetworkServer.ReplacePlayerForConnection(conn, player, playerControllerId);

		UpdatePlayerList();
	}

	private static void UpdatePlayerList()
	{
		Dictionary<string, GameObject> connectedPlayers = PlayerList.Instance.connectedPlayers;

		//Notify all clients that connected players list should be updated
		GameObject[] players = new GameObject[connectedPlayers.Count];
		connectedPlayers.Values.CopyTo(players, 0);
		UpdateConnectedPlayersMessage.Send(players);
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