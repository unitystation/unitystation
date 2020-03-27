using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_GhostOptions : MonoBehaviour
{
	[SerializeField] private Text ghostHearText = null;
	[SerializeField] private GameObject teleportButtonList = null;

	private bool TeleportScreenOpen = false;
	private bool PlacesTeleportScreenOpen = false;

	void OnEnable()
	{
		DetermineGhostHearText();
	}

	public void JumpToMob()
	{
		if (TeleportScreenOpen == true)// close screen if true
		{
			teleportButtonList.SetActive(false);
			TeleportScreenOpen = false;
		}
		else if (TeleportScreenOpen == false & PlacesTeleportScreenOpen == true)//switches to mob screen from places if true
		{
			TeleportScreenOpen = true;
			PlacesTeleportScreenOpen = false;
			GetComponentInChildren<TeleportButtonControl>().GenButtons();
		}
		else//opens screen
		{
			teleportButtonList.SetActive(true);
			TeleportScreenOpen = true;
			GetComponentInChildren<TeleportButtonControl>().GenButtons();
		}
	}

	public void Orbit()
	{
	}

	public void ReenterCorpse()
	{
		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdGhostCheck();
	}

	public void Teleport()
	{
		if (PlacesTeleportScreenOpen == true)//Close screen if true
		{
			teleportButtonList.SetActive(false);
			PlacesTeleportScreenOpen = false;
		}
		else if (PlacesTeleportScreenOpen == false & TeleportScreenOpen == true)// switches to Place Teleport if mob teleport is open
		{
			PlacesTeleportScreenOpen = true;
			TeleportScreenOpen = false;
			GetComponentInChildren<TeleportButtonControl>().PlacesGenButtons();
		}
		else//opens screen
		{
			teleportButtonList.SetActive(true);
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
	public void TeleportScreenClose()//closes screen by close button
	{
		teleportButtonList.SetActive(false);
		TeleportScreenOpen = false;
		PlacesTeleportScreenOpen = false;
	}
}