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
	private int count = 0;

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

		foreach (var pair in SoundManager.Instance.sounds)//sounds is a readonly so will never change hopefully
		{
			GameObject button = Instantiate(buttonTemplate) as GameObject;//creates new button
			button.SetActive(true);
			var c = button.GetComponent<AdminGlobalSoundButton>();
			c.SetAdminGlobalSoundButtonText(pair.Key);
			c.index = count;//Gives button a number, used to tell which data index is used for soundclip
			count += 1;
			soundButtons.Add(button);

			button.transform.SetParent(buttonTemplate.transform.parent, false);
		}
	}

	public void PlaySound(string index)//send sound to sound manager
	{
		if (SoundManager.Instance.sounds.ContainsKey(index))
		{
			SoundManager.PlayNetworked(index);
		}
	}
}
