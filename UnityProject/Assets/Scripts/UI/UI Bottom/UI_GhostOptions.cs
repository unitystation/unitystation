using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_GhostOptions : MonoBehaviour
{
	[SerializeField] private Text ghostHearText = null;

	private bool TeleportScreenOpen = false;
	private bool PlacesTeleportScreenOpen = false;

	void OnEnable()
	{
		DetermineGhostHearText();
	}

	public void JumpToMob()
	{
		if (TeleportScreenOpen == true)
		{
			transform.Find("TeleportButtonScrollList").gameObject.SetActive(false);
		}
		else if (TeleportScreenOpen == false & PlacesTeleportScreenOpen == true)
		{
			TeleportScreenOpen = true;
			GetComponentInChildren<TeleportButtonControl>().PlacesGenButtons();
		}
		else
		{
			transform.Find("TeleportButtonScrollList").gameObject.SetActive(true);
			TeleportScreenOpen = true;
			GetComponentInChildren<TeleportButtonControl>().GenButtons();
		}
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
		if (PlacesTeleportScreenOpen == true)
		{
			transform.Find("TeleportButtonScrollList").gameObject.SetActive(false);
		}
		else if (PlacesTeleportScreenOpen == false & TeleportScreenOpen == true)
		{
			PlacesTeleportScreenOpen = true;
			GetComponentInChildren<TeleportButtonControl>().PlacesGenButtons();
		}
		else
		{
			transform.Find("TeleportButtonScrollList").gameObject.SetActive(true);
			PlacesTeleportScreenOpen = true;
			GetComponentInChildren<TeleportButtonControl>().PlacesGenButtons();
		}
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

	//closes window.
	public void TeleportScreenClose()
	{
		transform.Find("TeleportButtonScrollList").gameObject.SetActive(false);
		TeleportScreenOpen = false;
		PlacesTeleportScreenOpen = false;
	}
}