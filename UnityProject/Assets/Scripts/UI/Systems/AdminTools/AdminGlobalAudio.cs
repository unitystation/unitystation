using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Audio.Containers;
using AddressableReferences;
using Managers;

namespace AdminTools
{
	/// <summary>
	/// Lets Admins play Audio
	/// </summary>
	public class AdminGlobalAudio : MonoBehaviour
	{
		[SerializeField] private GameObject buttonTemplate = null;
		[SerializeField] private Transform buttonList = null;
		private AdminGlobalAudioSearchBar SearchBar;
		public List<GameObject> audioButtons = new List<GameObject>();

		public List<SimpleAudioManager.SimpleAudioReference> simpleAudioList = new List<SimpleAudioManager.SimpleAudioReference>();

		private void Awake()
		{
			SearchBar = GetComponentInChildren<AdminGlobalAudioSearchBar>();
		}

		private void OnEnable()
		{
			LoadCustomMusic();
		}

		private void LoadCustomMusic()
		{
			simpleAudioList.Clear();
			foreach (var data in SimpleAudioManager.Instance.SharedData)
			{
				simpleAudioList.Add(data.Value);
			}
			LoadButtons();
		}

		/// <summary>
		/// Generates buttons for the audio list. Makes sure it's always up to date.
		/// </summary>
		private void LoadButtons()
		{
			foreach (var btn in audioButtons)
			{
				Destroy(btn);
			}
			if (SearchBar != null)
			{
				SearchBar.Resettext();
			}

			foreach (var audio in simpleAudioList)
			{
				GameObject button = Instantiate(buttonTemplate) as GameObject; //creates new button
				button.SetActive(true);
				AdminGlobalAudioButton buttonScript = button.GetComponent<AdminGlobalAudioButton>();
				buttonScript.Setup(this, audio.Data.FileTitle, audio.Data.ID);
				audioButtons.Add(button);
				button.transform.SetParent(buttonList, false);
			}
		}

		public virtual void PlayAudio(int index) {}
	}
}
