using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lets Admins play sounds
/// </summary>
public class AdminGlobalSound : MonoBehaviour
{
	[SerializeField]
	private GameObject buttonTemplate;
	private AdminGlobalSoundSearchBar SearchBar;
	public List<GameObject> soundButtons = new List<GameObject>();

	private void Awake()
	{
		SearchBar = GetComponentInChildren<AdminGlobalSoundSearchBar>();
		SoundList();
	}

	/// <summary>
	/// Generates buttons for the list
	/// </summary>
	public void SoundList()
	{
		if (SearchBar != null)
		{
			SearchBar.Resettext();
		}

		var sounds = SoundManager.Instance.GetComponentsInChildren<AudioSource>();

		foreach (AudioSource pair in sounds)//sounds is a readonly so will never change hopefully
		{
			if (!pair.loop)
			{
				GameObject button = Instantiate(buttonTemplate) as GameObject;//creates new button
				button.SetActive(true);
				button.GetComponent<AdminGlobalSoundButton>().SetAdminGlobalSoundButtonText(pair.gameObject.name);
				soundButtons.Add(button);

				button.transform.SetParent(buttonTemplate.transform.parent, false);
			}
		}
	}

	public void PlaySound(string index)//send sound to sound manager
	{
		var adminId = DatabaseAPI.ServerData.UserID;
		var adminToken = PlayerList.Instance.AdminToken;

		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdPlaySound(index, adminId, adminToken);
	}
}
