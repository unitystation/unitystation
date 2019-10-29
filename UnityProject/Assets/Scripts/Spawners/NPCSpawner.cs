using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns NPC's
/// Modify this as needed until we have a better idea
/// of how this spawner will be used before we do
/// a proper refactor
/// </summary>
public class NPCSpawner : MonoBehaviour
{
	public enum NPC
	{
		xeno
	}

	public enum SpawnerType
	{
		roundStartRandom
	}

	public SpawnerType spawnerType;

	public NPC npcToSpawn;

	void Start()
	{
		transform.localPosition = Vector3Int.RoundToInt(transform.localPosition);
		StartCoroutine(WaitForLoad());
	}

	IEnumerator WaitForLoad()
	{
		while (PoolManager.Instance == null || CustomNetworkManager.Instance == null)
		{
			yield return WaitFor.Seconds(1f);
		}

		yield return WaitFor.EndOfFrame;

		if (CustomNetworkManager.Instance._isServer)
		{
			OnStartServer();
		}
	}

	//Is ready on the server:
	void OnStartServer()
	{
		if (spawnerType == SpawnerType.roundStartRandom)
		{
			DoRandomSpawn();
		}
	}

	//Decides if it should spawn or not and how many npcs
	void DoRandomSpawn()
	{
		//50% chance
		if (Random.value > 0.5f)
		{
			var amountToSpawn = Random.Range(1, 3);
			for (int i = 0; i < amountToSpawn; i++)
			{
				switch (npcToSpawn)
				{
					case NPC.xeno:
						NPCFactory.SpawnXenomorph(transform.position, transform.parent);
						break;
				}
			}
		}
	}
}