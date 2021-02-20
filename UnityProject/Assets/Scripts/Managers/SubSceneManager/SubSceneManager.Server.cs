using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

//Server
public partial class SubSceneManager
{
	public override void OnStartServer()
	{
		// Determine a Main station subscene and away site
		StartCoroutine(RoundStartServerLoadSequence());
		base.OnStartServer();
	}

	/// Sync init data with specific scenes
	/// staggered over multiple frames
	public IEnumerator SyncPlayerData(NetworkConnection connToAdd, Scene sceneContext)
	{

		var client = connToAdd.clientOwnedObjects.Count == 0 ? null : connToAdd.clientOwnedObjects.ElementAt(0).gameObject;

		Logger.LogFormat("SyncPlayerData. This server sending a bunch of sync data to new " +
		                 "client {0} for scene {1}", Category.Connections, client, sceneContext.name);

		//Add connection as observer to the scene objects:
		yield break;
	}
}
