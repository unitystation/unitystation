using UnityEngine;
using UnityEngine.Networking;

public class SoundNetworkActions : NetworkBehaviour
{
	[Command]
	public void CmdPlaySound(string soundName, Vector3 pos)
	{
		RpcPlayNetworkSound(soundName, pos);
	}

	[Command]
	public void CmdPlaySoundAtPlayerPos(string soundName)
	{
		RpcPlayNetworkSound(soundName, transform.position);
	}

	[ClientRpc]
	public void RpcPlayNetworkSound(string soundName, Vector3 pos)
	{
		SoundManager.PlayAtPosition(soundName, pos);
	}
}