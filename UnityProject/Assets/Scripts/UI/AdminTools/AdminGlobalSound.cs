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
	public List<AudioSource> soundClips = new List<AudioSource>();
	public List<GameObject> soundButtons = new List<GameObject>();

	private void Start()
	{
		foreach (AudioSource source in SoundManager.Instance.gameObject.GetComponentsInChildren<AudioSource>())
		{
			soundClips.Add(source);
			Logger.Log(source.name);
			Logger.Log(source.gameObject.transform.name);
		}

		SearchBar = GetComponentInChildren<AdminGlobalSoundSearchBar>();
		SoundList();
	}

	/// <summary>
	/// Generates buttons
	/// </summary>
	public void SoundList()
	{
		if (SearchBar != null)
		{
			SearchBar.Resettext();
		}

		for (int i = 0; i < soundClips.Count; i++)
		{
			GameObject button = Instantiate(buttonTemplate) as GameObject;//creates new button
			button.SetActive(true);
			var c = button.GetComponent<AdminGlobalSoundButton>();
			c.SetAdminGlobalSoundButtonText(soundClips[i].gameObject.transform.name);
			c.index = i;//Gives button a number, used to tell which data index is used for soundclip
			soundButtons.Add(button);

			button.transform.SetParent(buttonTemplate.transform.parent, false);
		}
	}

	public void PlaySound(int index)
	{
		SoundManager.PlayNetworked(soundClips[index].gameObject.transform.name, 1f);
	}
}
