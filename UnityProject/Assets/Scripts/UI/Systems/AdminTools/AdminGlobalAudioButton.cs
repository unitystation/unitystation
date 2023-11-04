using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AdminTools
{
	/// <summary>
	/// On the buttons in the list
	/// </summary>
	public class AdminGlobalAudioButton : MonoBehaviour
	{
		public Text myText;

		public string SoundAddress;

		public void SetText(string textString)
		{
			myText.text = textString;
		}

		public void Onclick()
		{
			var adminGlobalAudio = GetComponentInParent<AdminGlobalAudio>();

			adminGlobalAudio.PlayAudio(SoundAddress); // Gives text to function to play audio.
		}
	}
}
