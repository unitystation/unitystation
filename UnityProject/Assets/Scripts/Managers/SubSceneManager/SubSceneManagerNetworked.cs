using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class SubSceneManagerNetworked : NetworkBehaviour
{
	public readonly ScenesSyncList loadedScenesList = new ScenesSyncList();

	public SubSceneManager SubSceneManager;

	public override void OnStartServer()
	{
		NetworkServer.observerSceneList.Clear();
		// Determine a Main station subscene and away site
		StartCoroutine(SubSceneManager.RoundStartServerLoadSequence());
		base.OnStartServer();
	}

}
