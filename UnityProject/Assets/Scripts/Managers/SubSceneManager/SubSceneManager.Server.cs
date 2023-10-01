using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Logs;
using Messages.Server;
using Mirror;
using Tilemaps.Behaviours.Layers;
using UnityEngine;
using UnityEngine.SceneManagement;

//Server
public partial class SubSceneManager
{


	/// <summary>
	/// Starts a collection of scenes that this connection is allowed to see
	/// </summary>
	public void AddNewObserverScenePermissions(NetworkConnectionToClient conn)
	{
		if (NetworkServer.observerSceneList.ContainsKey(conn))
		{
			RemoveSceneObserver(conn);
		}

		NetworkServer.observerSceneList.Add(conn, new List<Scene> {SceneManager.GetActiveScene()});
	}

	public void RemoveSceneObserver(NetworkConnectionToClient conn)
	{
		NetworkServer.observerSceneList.Remove(conn);
	}

	/// <summary>
	/// No scene / proximity visibility checking. Just adding it to everything
	/// </summary>
	/// <param name="connToAdd"></param>
	void AddObserverToAllObjects(NetworkConnectionToClient connToAdd, Scene sceneContext)
	{
		StartCoroutine(SyncPlayerData(connToAdd, sceneContext));
		AddObservableSceneToConnection(connToAdd, sceneContext);
	}

	/// Sync init data with specific scenes
	/// staggered over multiple frames
	public IEnumerator SyncPlayerData(NetworkConnectionToClient connToAdd, Scene sceneContext)
	{

		var client = connToAdd.clientOwnedObjects.Count == 0 ? null : connToAdd.clientOwnedObjects.ElementAt(0).gameObject;

		Loggy.LogFormat("SyncPlayerData. This server sending a bunch of sync data to new " +
		                 "client {0} for scene {1}", Category.Connections, client, sceneContext.name);

		//Add connection as observer to the scene objects:
		yield return StartCoroutine(AddObserversForClient(connToAdd, sceneContext));
	}

	/// <summary>
	/// Add a connection as an observer to everything in a scene
	/// </summary>
	IEnumerator AddObserversForClient(NetworkConnectionToClient connToAdd, Scene sceneContext)
	{
		//Activate the matrices on the client first
		foreach (var matrixInfo in MatrixManager.Instance.ActiveMatrices.Values)
		{
			if (matrixInfo.Matrix.gameObject.scene == sceneContext)
			{
				matrixInfo.Matrix.GetComponentInParent<NetworkedMatrix>().MatrixSync.netIdentity.AddPlayerObserver(connToAdd);
			}
		}

		yield return WaitFor.EndOfFrame;

		var Stopwatch = new Stopwatch();

		var objCount = 0;
		var netIds = NetworkServer.spawned.Values.ToList();
		Stopwatch.Start();
		foreach (var n in netIds)
		{
			if (n == null) continue;

			if (n.gameObject.scene != sceneContext)
				continue;

			if (connToAdd.identity == null) //connection dc midway, we're out
				yield break;

			n.AddPlayerObserver(connToAdd);

			if (Stopwatch.ElapsedMilliseconds >= 10)
			{
				Stopwatch.Reset();
				yield return WaitFor.EndOfFrame;
				Stopwatch.Start();
			}
		}

		yield return null;
		FinishedAddedObserverMessage.Send(connToAdd , sceneContext.name);
	}
}
