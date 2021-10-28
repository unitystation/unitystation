using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdminCommands;
using DatabaseAPI;
using Audio.Containers;
using System.Threading.Tasks;
using AddressableReferences;


namespace AdminTools
{
	/// <summary>
	/// Lets Admins play Audio
	/// </summary>
	public class AdminGlobalAudio : MonoBehaviour
	{
		[SerializeField] private GameObject buttonTemplate = null;
		private AdminGlobalAudioSearchBar SearchBar;
		public List<GameObject> audioButtons = new List<GameObject>();

		[SerializeField] private AudioClipsArray audioAddressables = null;

		public List<AddressableAudioSource> audioList;

		private void Awake()
		{
			SearchBar = GetComponentInChildren<AdminGlobalAudioSearchBar>();
			
			DoLoadAudio(audioAddressables);
		}

		private void DoLoadAudio(AudioClipsArray audioAddressables)
		{
			audioList = new List<AddressableAudioSource>();

			async Task LoadAudio()
			{
				foreach (var audioSource in audioAddressables.AddressableAudioSource)
				{
					var audio = await AudioManager.GetAddressableAudioSourceFromCache(audioSource);
					audioList.Add(audio);
				}
				LoadButtons();
			}
			
			LoadAudio();
		}

		/// <summary>
		/// Generates buttons for the audio list
		/// </summary>
		private void LoadButtons()
		{
			if (SearchBar != null)
			{
				SearchBar.Resettext();
			}

			int index = 0;
			foreach (AddressableAudioSource audio in audioList)
			{
				AudioSource source = audio.AudioSource;
				if (!source.loop)
				{
					GameObject button = Instantiate(buttonTemplate) as GameObject; //creates new button
					button.SetActive(true);
					AdminGlobalAudioButton buttonScript = button.GetComponent<AdminGlobalAudioButton>();
					buttonScript.SetText(source.clip.name);
					buttonScript.SetIndex(index++);
					audioButtons.Add(button);
					button.transform.SetParent(buttonTemplate.transform.parent, false);
				}
			}
		}

		public virtual void PlayAudio(int index) {}
	}
}
