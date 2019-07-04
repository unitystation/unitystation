using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_GhostOptions : MonoBehaviour
{

	public void JumpToMob()
	{

	}
	public void Orbit()
	{

	}
	public void ReenterCorpse()
	{
		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdEnterBody();
	}
	public void Teleport()
	{

	}
	public void pAIcandidate()
	{

	}
	public void Respawn()
	{
		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRespawnPlayer();
	}

	public void ToggleAllowCloning()
	{
		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleAllowCloning();
	}
}
