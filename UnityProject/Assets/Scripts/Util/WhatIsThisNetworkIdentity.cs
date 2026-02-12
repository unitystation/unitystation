using Logs;
using Mirror;
using UnityEngine;
using US13.Managers.NetworkManagement;

namespace Util
{
	public class WhatIsThisNetworkIdentity : MonoBehaviour
	{
		public uint ID = 0;

		[NaughtyAttributes.Button()]
		public void WhatIsThis()
		{
			var spawned =
				CustomNetworkManager.IsServer ? NetworkServer.spawned : NetworkClient.spawned;

			Loggy.Error(spawned[ID].gameObject.name);
		}
	}
}
