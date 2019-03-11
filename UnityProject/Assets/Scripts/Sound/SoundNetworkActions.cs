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

	[Command]
	public void CmdPlaySoundAtPlayerPos(string soundName)
	{
		RpcPlayNetworkSoundWithPitch(soundName, transform.position, 1f);
	}

	[ClientRpc]
	public void RpcPlayNetworkSoundWithPitch(string soundName, Vector3 pos, float pitch)
	{
		SoundManager.PlayAtPosition(soundName, pos, pitch);
	}

	[ClientRpc]
	public void RpcPlayNetworkSound(string soundName, Vector3 pos)
	{
		SoundManager.PlayAtPosition(soundName, pos, 1f);
	}
}