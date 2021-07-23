using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomNetSceneChecker : NetworkVisibility
{
	//TODO this component has now been added to all
	//spawnable prefabs. It is a placeholder to be used for
	//scene observer rebuilding when we start to prevent connections
	//from observing scenes that they are not apart of

	public override bool OnCheckObserver(NetworkConnection conn)
	{
		//CUSTOM UNITYSTATION CODE//
		if (NetworkServer.observerSceneList.ContainsKey(conn) &&
		    NetworkServer.observerSceneList[conn].Contains(gameObject.scene))
		{
			return true;
		}

		return SceneManager.GetActiveScene() == gameObject.scene;
	}

	public override void OnRebuildObservers(HashSet<NetworkConnection> observers, bool initialize)
	{
		// foreach (var player in PlayerList.Instance.AllPlayers)
		// {
			// observers.Add(player.Connection);
		// }


		//return;
		//CUSTOM UNITYSTATION CODE//
		foreach (var obs in NetworkServer.observerSceneList)
		{
			if (obs.Key == null) continue;
			if (gameObject.scene == SceneManager.GetActiveScene())
			{
				observers.Add(obs.Key);
			}
			else
			{
				if (obs.Value.Contains(gameObject.scene))
				{
					observers.Add(obs.Key);
				}
			}
		}
	}

	public override void OnSetHostVisibility(bool visible)
	{
		//CUSTOM UNITYSTATION CODE//
		//Reenable when testing out proximity stuff so that it works in editor as host

		//foreach (Renderer rend in GetComponentsInChildren<Renderer>())
		//    rend.enabled = visible;
	}
}
