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
		//All matrices
		yield return StartCoroutine(InitializeMatricesForClient(connToAdd, sceneContext));
		//Sync Tilemaps
		yield return StartCoroutine(InitializeTileMapsForClient(connToAdd, sceneContext));
		//All transforms
		yield return StartCoroutine(InitializeTransformsForClient(connToAdd, sceneContext));
		//All Players
		yield return StartCoroutine(InitializePlayersForClient(connToAdd, sceneContext));
		//All Doors
		yield return StartCoroutine(InitializeDoorsForClient(connToAdd, sceneContext));
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

		var netIds = NetworkIdentity.spawned.Values.ToList();
		foreach (var n in netIds)
		{
			if (n.gameObject.scene != sceneContext) continue;

			n.AddPlayerObserver(connToAdd);
		}

		yield return WaitFor.EndOfFrame;
	}

	//Force a notify update for all tilemaps in a given scene to a client connection
	IEnumerator InitializeTileMapsForClient(NetworkConnection connToAdd, Scene sceneContext)
	{
		//TileChange Data
		TileChangeManager[] tcManagers = FindObjectsOfType<TileChangeManager>();
		for (var i = 0; i < tcManagers.Length; i++)
		{
			if(tcManagers[i] == null ||
			   tcManagers[i].gameObject.scene != sceneContext) continue;

			var playerOwnedObj = ClientOwnedObject(connToAdd);
			if (playerOwnedObj != null)
			{
				tcManagers[i].NotifyPlayer(playerOwnedObj);
			}
			yield return WaitFor.EndOfFrame;
		}

		yield return WaitFor.EndOfFrame;
	}

	//Force a notify update for all Matrices in a given scene to a client connection
	IEnumerator InitializeMatricesForClient(NetworkConnection connToAdd, Scene sceneContext)
	{
		MatrixMove[] matrices = FindObjectsOfType<MatrixMove>();
		for (var i = 0; i < matrices.Length; i++)
		{
			if(matrices[i].gameObject.scene != sceneContext) continue;
			var playerOwnedObj = ClientOwnedObject(connToAdd);
			if(playerOwnedObj != null) matrices[i].NotifyPlayer(playerOwnedObj, true);
		}

		yield return WaitFor.EndOfFrame;
	}

	//Force a notify update for all Transforms in a given scene to a client connection
	IEnumerator InitializeTransformsForClient(NetworkConnection connToAdd, Scene sceneContext)
	{
		var objCount = 0;
		CustomNetTransform[] scripts = FindObjectsOfType<CustomNetTransform>();
		for (var i = 0; i < scripts.Length; i++)
		{
			if (scripts[i] == null ||
			    scripts[i].gameObject.scene != sceneContext) continue;
			var playerOwnedObj = ClientOwnedObject(connToAdd);
			if (playerOwnedObj != null) scripts[i].NotifyPlayer(playerOwnedObj);
			//We are trying to limit physics 2d spikes on the client
			//20 notifys a frame:
			objCount += 1;
			if (objCount >= 10)
			{
				objCount = 0;
				yield return WaitFor.EndOfFrame;
			}
		}

		yield return WaitFor.EndOfFrame;
	}

	//Force a notify update for all Players and sprites in a given scene to a client connection
	IEnumerator InitializePlayersForClient(NetworkConnection connToAdd, Scene sceneContext)
	{
		//All player bodies
		PlayerSync[] playerBodies = FindObjectsOfType<PlayerSync>();
		for (var i = 0; i < playerBodies.Length; i++)
		{
			if (playerBodies[i].gameObject.scene != sceneContext) continue;
			var playerBody = playerBodies[i];
			var playerOwnedObj = ClientOwnedObject(connToAdd);
			if (playerOwnedObj != null) playerBody.NotifyPlayer(playerOwnedObj, true);
			var playerSprites = playerBody.GetComponent<PlayerSprites>();
			if (playerSprites)
			{
				if (playerOwnedObj != null) playerSprites.NotifyPlayer(playerOwnedObj);
			}
			var equipment = playerBody.GetComponent<Equipment>();
			if (equipment)
			{
				if (playerOwnedObj != null) equipment.NotifyPlayer(playerOwnedObj);
			}
		}

		yield return WaitFor.EndOfFrame;
	}

	//Force a notify update for all Doors in a given scene to a client connection
	IEnumerator InitializeDoorsForClient(NetworkConnection connToAdd, Scene sceneContext)
	{
		//Doors
		DoorController[] doors = FindObjectsOfType<DoorController>();
		for (var i = 0; i < doors.Length; i++)
		{
			if (doors[i].gameObject.scene != sceneContext) continue;
			var playerOwnedObj = ClientOwnedObject(connToAdd);
			if (playerOwnedObj != null) doors[i].NotifyPlayer(playerOwnedObj);
		}

		yield return WaitFor.EndOfFrame;
	}

	public GameObject ClientOwnedObject(NetworkConnection conn)
	{
		if (conn == null || conn.clientOwnedObjects.Count == 0) return null;
		return conn.clientOwnedObjects.ElementAt(0).gameObject;
	}
}
