using System.Linq;
using PlayGroup;
using Tilemaps.Scripts.Behaviours.Layers;
using UnityEngine;
using UnityEngine.Networking;

public static class SpawnHandler
{
    private static readonly CustomNetworkManager networkManager = CustomNetworkManager.Instance;

    public static void SpawnPlayer(NetworkConnection conn, short playerControllerId, JobType jobType = JobType.NULL)
    {
        var player = CreatePlayer(jobType);
        NetworkServer.AddPlayerForConnection(conn, player, playerControllerId);

        var connectedPlayers = PlayerList.Instance.connectedPlayers;

        //Notify all clients that connected players list should be updated
        var players = new GameObject[connectedPlayers.Count];
        connectedPlayers.Values.CopyTo(players, 0);
        UpdateConnectedPlayersMessage.Send(players);
    }

    public static void RespawnPlayer(NetworkConnection conn, short playerControllerId, JobType jobType)
    {
        var player = CreatePlayer(jobType);
        NetworkServer.ReplacePlayerForConnection(conn, player, playerControllerId);

        var connectedPlayers = PlayerList.Instance.connectedPlayers;

        //Notify all clients that connected players list should be updated
        var players = new GameObject[connectedPlayers.Count];
        connectedPlayers.Values.CopyTo(players, 0);
        UpdateConnectedPlayersMessage.Send(players);
    }

    private static GameObject CreatePlayer(JobType jobType)
    {
        var playerPrefab = networkManager.playerPrefab;

        var spawnPosition = GetSpawnForJob(jobType);

        GameObject player;

        if (spawnPosition != null)
        {
            var position = spawnPosition.position;
            var rotation = spawnPosition.rotation;
            var parent = spawnPosition.GetComponentInParent<ObjectLayer>().transform;
            player = Object.Instantiate(playerPrefab, position, rotation, parent);
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

        var spawnPoints = networkManager.startPositions.Select(x => x.GetComponent<SpawnPoint>())
            .Where(x => x.JobRestrictions.Contains(jobType)).ToList();

        return spawnPoints.Count == 0 ? null : spawnPoints.PickRandom().transform;
    }
}