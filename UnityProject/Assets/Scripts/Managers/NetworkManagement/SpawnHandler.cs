using System.Collections.Generic;
using System.Linq;
using PlayGroup;
using Tilemaps.Scripts.Behaviours.Layers;
using UnityEngine;
using UnityEngine.Networking;

public static class SpawnHandler
{
    private static CustomNetworkManager networkManager = CustomNetworkManager.Instance;

    public static void SpawnPlayer(NetworkConnection conn, short playerControllerId, JobType jobType = JobType.NULL)
    {
        NetworkServer.AddPlayerForConnection(conn, CreatePlayer(jobType), playerControllerId);
    }

    public static void RespawnPlayer(NetworkConnection conn, short playerControllerId, JobType jobType)
    {
        NetworkServer.ReplacePlayerForConnection(conn,  CreatePlayer(jobType), playerControllerId);
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

        var spawnPoints = networkManager.startPositions.Select(x => x.GetComponent<SpawnPoint>()).Where(x => x.JobRestrictions.Contains(jobType)).ToList();
        
        return spawnPoints.Count == 0 ? null : spawnPoints.PickRandom().transform;
    }
}
