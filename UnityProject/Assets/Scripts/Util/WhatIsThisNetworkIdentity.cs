using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class WhatIsThisNetworkIdentity : MonoBehaviour
{
	public uint ID = 0;

	[NaughtyAttributes.Button()]
	public void WhatIsThis()
	{
		Logger.LogError(NetworkIdentity.spawned[ID].gameObject.name);
	}
}
