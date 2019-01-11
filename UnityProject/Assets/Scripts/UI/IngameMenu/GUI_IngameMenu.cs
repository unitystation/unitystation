using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class GUI_IngameMenu : MonoBehaviour
{
	public GameObject mainIngameMenu;
	public GameObject generalSettingsMenu;
	public GameObject controlSettingsMenu;

	private ModalPanelManager modalPanelManager => ModalPanelManager.Instance;

	private CustomNetworkManager networkManager => CustomNetworkManager.Instance;
	public static GUI_IngameMenu Instance;

	// MonoBehaviour Functions
	// ==================================================
	void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}
	}

	// Main Ingame Menu Functions
	// ==================================================
	public void OpenMenuPanel(GameObject nextMenuPanel)
	{
		SoundManager.Play("Click01");
		Logger.Log("Opening " + nextMenuPanel.name + " menu", Category.UI);
		nextMenuPanel.SetActive(true);
	}
	public void CloseMenuPanel(GameObject thisPanel)
	{
		SoundManager.Play("Click01");
		Logger.Log("Closing " + thisPanel.name + " menu", Category.UI);
		thisPanel.SetActive(false);
	}

	// Logout confirmation window functions
	// ==================================================
	public void LogoutButton()
	{
		modalPanelManager.Confirm("Are you sure?", LogoutConfirmYesButton, "Logout");
	}
	public void LogoutConfirmYesButton()
	{
		SoundManager.Play("Click01");
		HideAllMenus();
		StopNetworking();
		SceneManager.LoadScene("Lobby");
	}

	// Exit confirmation window functions
	// ==================================================
	public void ExitButton()
	{
		modalPanelManager.Confirm("Are you sure?", ExitConfirmYesButton, "Exit");
	}
	public void ExitConfirmYesButton()
	{
		SoundManager.Play("Click01");
		StopNetworking();
		// Either shutdown the application or stop the editor
		#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
		#else
			Application.Quit();
		#endif
	}
	
	// Misc functions
	// ==================================================
	private void StopNetworking()
	{
		// Check if a host or regular client is shutting down
		if (networkManager._isServer)
		{
			networkManager.StopHost();
			Logger.Log("Stopping host", Category.Connections);
		}
		else
		{
			networkManager.StopClient();
			Logger.Log("Stopping client", Category.Connections);
		}
	}
	private void HideAllMenus()
	{
		mainIngameMenu.SetActive(false);
		generalSettingsMenu.SetActive(false);
		controlSettingsMenu.SetActive(false);
	}
}
