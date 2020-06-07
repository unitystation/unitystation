using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Random = UnityEngine.Random;

/// <summary>
/// Used for directional windows, based on WindowFullTileObject.
/// </summary>
public class WindowDirectionalObject : NetworkBehaviour
{
	private RegisterObject registerObject;
	private ObjectBehaviour objectBehaviour;
	
	//PM: Objects below don't have to be shards or rods, but it's more convenient for me to put "shards" and "rods" in the variable names.

	[Header("Destroyed variables.")]
	[Tooltip("Drops this when broken with force.")]
	public GameObject shardsOnDestroy;

	[Tooltip("Drops this count when destroyed.")]
	public int minCountOfShardsOnDestroy;

	[Tooltip("Drops this count when destroyed.")]
	public int maxCountOfShardsOnDestroy;

	[Tooltip("Drops this when broken with force.")]
	public GameObject rodsOnDestroy;
	
	[Tooltip("Drops this count when destroyed.")]
	public int minCountOfRodsOnDestroy;

	[Tooltip("Drops this count when destroyed.")]
	public int maxCountOfRodsOnDestroy;
	
	[Tooltip("Sound when destroyed.")]
	public string soundOnDestroy;



	private void Start()
	{
		registerObject = GetComponent<RegisterObject>();
		GetComponent<Integrity>().OnWillDestroyServer.AddListener(OnWillDestroyServer);
		objectBehaviour = GetComponent<ObjectBehaviour>();
	}


	private void OnWillDestroyServer(DestructionInfo arg0)
	{
		Spawn.ServerPrefab(shardsOnDestroy, gameObject.TileWorldPosition().To3Int(), transform.parent, count: Random.Range(minCountOfShardsOnDestroy, maxCountOfShardsOnDestroy + 1),
			scatterRadius: Random.Range(0, 3), cancelIfImpassable: true);
			
		Spawn.ServerPrefab(rodsOnDestroy, gameObject.TileWorldPosition().To3Int(), transform.parent, count: Random.Range(minCountOfRodsOnDestroy, maxCountOfRodsOnDestroy + 1),
			scatterRadius: Random.Range(0, 3), cancelIfImpassable: true);

		SoundManager.PlayNetworkedAtPos(soundOnDestroy, gameObject.TileWorldPosition().To3Int(), 1f, sourceObj: gameObject);
	}
}
