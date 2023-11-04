using System.Collections;
using System.Collections.Generic;
using Logs;
using Mirror;
using UnityEngine;

public class WhatIsThisNetworkIdentity : MonoBehaviour
{
	public uint ID = 0;

	[NaughtyAttributes.Button()]
	public void WhatIsThis()
	{
		var spawned =
			CustomNetworkManager.IsServer ? NetworkServer.spawned : NetworkClient.spawned;

		Loggy.LogError(spawned[ID].gameObject.name);
	}
}
