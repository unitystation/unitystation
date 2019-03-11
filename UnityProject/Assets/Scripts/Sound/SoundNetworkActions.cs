using UnityEngine;
using UnityEngine.Networking;

public class SoundNetworkActions : NetworkBehaviour
{
	[Command]
	public void CmdPlaySound(string soundName, Vector3 pos)
	{
		RpcPlayNetworkSound(soundName, pos);
	}

	// fixme: unsecure af, lets client play arbitrary sounds at will ^v

	[Command] [System.Obsolete("Use PlaySoundMessage instead")]
	public void CmdPlaySoundAtPlayerPos(string soundName)
	{
		RpcPlayNetworkSound(soundName, transform.position);
	}

	[ClientRpc]
	public void RpcPlayNetworkSound(string soundName, Vector3 pos)
	{
		SoundManager.PlayAtPosition(soundName, pos, 1f);
	}
}