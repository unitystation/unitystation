using System.Collections;
using System.Collections.Generic;
using Initialisation;
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
		LoadManager.RegisterAction(Init);
	}

	void Init()
	{
		transform.localPosition = Vector3Int.RoundToInt(transform.localPosition);
		this.WaitForNetworkManager(OnStartServer);
	}
	//Is ready on the server:
	void OnStartServer()
	{
		if (CustomNetworkManager.Instance.isServer)
		{
			if (spawnerType == SpawnerType.roundStartRandom)
			{
				DoRandomSpawn();
			}
		}
	}

	//Decides if it should spawn or not and how many npcs
	void DoRandomSpawn()
	{
		//50% chance
		if (Random.value > 0.5f)
		{
			var amountToSpawn = Random.Range(1, 4);
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