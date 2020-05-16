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
		NetworkServer.observerSceneList.Clear();
		// Determine a Main station subscene and away site
		StartCoroutine(RoundStartServerLoadSequence());
		base.OnStartServer();
	}

	/// <summary>
	/// Starts a collection of scenes that this connection is allowed to see
	/// </summary>
	public void AddNewObserverScenePermissions(NetworkConnection conn)
	{
		if (NetworkServer.observerSceneList.ContainsKey(conn))
		{
			NetworkServer.observerSceneList.Remove(conn);
		}

		NetworkServer.observerSceneList.Add(conn, new List<Scene> {SceneManager.GetActiveScene()});
	}

	/// <summary>
	/// No scene / proximity visibility checking. Just adding it to everything
	/// </summary>
	/// <param name="connToAdd"></param>
	void AddObserverToAllObjects(NetworkConnection connToAdd, Scene sceneContext)
	{
		StartCoroutine(SyncPlayerData(connToAdd, sceneContext));
		AddObservableSceneToConnection(connToAdd, sceneContext);
	}

	/// Sync init data with specific scenes
	/// staggered over multiple frames
	public IEnumerator SyncPlayerData(NetworkConnection connToAdd, Scene sceneContext)
	{
		Logger.LogFormat("SyncPlayerData. This server sending a bunch of sync data to new " +
		                 "client {0} for scene {1}", Category.Connections, connToAdd.clientOwnedObjects.ElementAt(0).gameObject, sceneContext.name);

		//Add connection as observer to the scene objects:
		yield return StartCoroutine(AddObserversForClient(connToAdd, sceneContext));
	}

	/// <summary>
	/// Add a connection as an observer to everything in a scene
	/// </summary>
	IEnumerator AddObserversForClient(NetworkConnection connToAdd, Scene sceneContext)
	{
		//Activate the matrices on the client first
		for (int i = 0; i < MatrixManager.Instance.ActiveMatrices.Count; i++)
		{
			var matrix = MatrixManager.Instance.ActiveMatrices[i].Matrix;
			if (matrix.gameObject.scene == sceneContext)
			{
				matrix.GetComponentInParent<NetworkIdentity>().AddPlayerObserver(connToAdd);
			}
		}

		var objCount = 0;
		var netIds = NetworkIdentity.spawned.Values.ToList();
		foreach (var n in netIds)
		{
			if (n.gameObject.scene != sceneContext) continue;

			n.AddPlayerObserver(connToAdd);
			objCount++;
			if (objCount >= 20)
			{
				objCount = 0;
				yield return WaitFor.EndOfFrame;
			}
		}

		yield return WaitFor.EndOfFrame;
	}
}
