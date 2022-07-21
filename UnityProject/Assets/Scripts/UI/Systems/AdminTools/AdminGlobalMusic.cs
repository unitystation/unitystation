using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdminCommands;
using DatabaseAPI;
using Audio.Containers;
using System.Threading.Tasks;
using AddressableReferences;
using Managers;


namespace AdminTools
{
	/// <summary>
	/// Lets Admins play music
	/// </summary>
	public class AdminGlobalMusic : AdminGlobalAudio
	{
		[SerializeField] private GameObject newSoundUI;
		public override void PlayAudio(int index) //send music to audio manager
		{
			SimpleAudioManager.PlayGlobally(index);
		}

		public void OnClickAddSound()
		{
			newSoundUI.SetActive(true);
			this.SetActive(false);
		}
	}
}
