using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Mirror;

namespace Core.Networking
{
	// TODO: there's an all-new SceneInterestManagement Mirror class. Can we use that instead?
	public class CustomInterestManagement : InterestManagement
	{
		// TODO: this is a placeholder to be used for
		// scene observer rebuilding when we start to prevent connections
		// from observing scenes that they are not apart of

		public override bool OnCheckObserver(NetworkIdentity identity, NetworkConnectionToClient conn)
		{
			if (NetworkServer.observerSceneList.ContainsKey(conn) &&
				NetworkServer.observerSceneList[conn].Contains(identity.gameObject.scene))
			{
				return true;
			}

			return SceneManager.GetActiveScene() == identity.gameObject.scene;
		}

		public override void OnRebuildObservers(NetworkIdentity identity, HashSet<NetworkConnectionToClient> newObservers)
		{
			foreach (var obs in NetworkServer.observerSceneList)
			{
				if (obs.Key == null) continue;
				if (identity.gameObject.scene == SceneManager.GetActiveScene())
				{
					newObservers.Add(obs.Key);
				}
				else
				{
					if (obs.Value.Contains(identity.gameObject.scene))
					{
						newObservers.Add(obs.Key);
					}
				}
			}
		}

		public override void SetHostVisibility(NetworkIdentity identity, bool visible)
		{
			// Re-enable when testing out proximity stuff so that it works in editor as host.

			//foreach (Renderer rend in identity.GetComponentsInChildren<Renderer>())
			//    rend.enabled = visible;
		}
	}
}
