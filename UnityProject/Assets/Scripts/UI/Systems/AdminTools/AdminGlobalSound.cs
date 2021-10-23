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
	/// Lets Admins play sounds
	/// </summary>
	public class AdminGlobalSound : MonoBehaviour
	{
		[SerializeField] private GameObject buttonTemplate = null;
		private AdminGlobalSoundSearchBar SearchBar;
		public List<GameObject> soundButtons = new List<GameObject>();

		[SerializeField] private AudioClipsArray musicAddressables = null;
		[SerializeField] private AudioClipsArray soundAddressables = null;

		public List<AddressableAudioSource> musicList;
		public List<AddressableAudioSource> soundList;

		private void Awake()
		{
			SearchBar = GetComponentInChildren<AdminGlobalSoundSearchBar>();
			
			musicList = DoLoadAudio(musicAddressables);
			soundList = DoLoadAudio(soundAddressables);
		}

		private List<AddressableAudioSource> DoLoadAudio(AudioClipsArray audioAddressables)
		{
			var audioList = new List<AddressableAudioSource>();

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
			return audioList;
		}

		/// <summary>
		/// Generates buttons for the sound list
		/// </summary>
		private void LoadButtons()
		{
			if (SearchBar != null)
			{
				SearchBar.Resettext();
			}

			int index = 0;
			foreach (AddressableAudioSource sound in soundList)
			{
				AudioSource source = sound.AudioSource;
				if (!source.loop)
				{
					GameObject button = Instantiate(buttonTemplate) as GameObject; //creates new button
					button.SetActive(true);
					AdminGlobalSoundButton buttonScript = button.GetComponent<AdminGlobalSoundButton>();
					buttonScript.SetText(source.clip.name);
					buttonScript.SetIndex(index++);
					soundButtons.Add(button);
					button.transform.SetParent(buttonTemplate.transform.parent, false);
				}
			}
		}

		public void PlaySound(int index) //send sound to sound manager
		{
			if (index < soundList.Count)
			{
				AdminCommandsManager.Instance.CmdPlaySound(soundList[index]);
			}
		}

		public void PlayMusic(string index)
		{

		}
	}
}
