using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ShowNetId : NetworkBehaviour
{

	public uint netId2;
	
	// Update is called once per frame
	void Update () {
		netId2 = netId.Value;
	}
}
