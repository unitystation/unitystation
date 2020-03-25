using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_GhostOptions : MonoBehaviour
{
	[SerializeField] private Text ghostHearText = null;

	private bool TeleportScreenOpen = false;

	void OnEnable()
	{
		DetermineGhostHearText();
	}

	public void JumpToMob()
	{
		TeleportScreenStatus();
	}

	public void Orbit()
	{
	}

	public void ReenterCorpse()
	{
		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdGhostEnterBody();
	}

	public void Teleport()
	{
		TeleportScreenStatus();
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

	public void ToggleGhostHearRange()
	{
		Chat.Instance.GhostHearAll = !Chat.Instance.GhostHearAll;
		DetermineGhostHearText();
	}

	void DetermineGhostHearText()
	{
		if (Chat.Instance.GhostHearAll)
		{
			ghostHearText.text = "HEAR\r\n \r\nLOCAL";
		}
		else
		{
			ghostHearText.text = "HEAR\r\n \r\nALL";
		}
	}

	//Checking to see if window is open and whether to close it.
	public void TeleportScreenStatus()
	{
		if (!TeleportScreenOpen)
		{
			transform.Find("TeleportButtonScrollList").gameObject.SetActive(true);
			TeleportScreenOpen = true;
			GetComponentInChildren<TeleportButtonControl>().GenButtons();
		}
		else
		{
			transform.Find("TeleportButtonScrollList").gameObject.SetActive(false);
			TeleportScreenOpen = false;
		}
	}
}