using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SoundNetworkActions : NetworkBehaviour {

	[Command]
	public void CmdPlayNetworkSound(string soundName, Vector3 pos){
		RpcPlayNetworkSound(soundName, pos);
	}

	[ClientRpc]
	void RpcPlayNetworkSound(string soundName, Vector3 pos){
		SoundManager.PlayAtPosition(soundName, pos);
	}
}
