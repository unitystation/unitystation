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
		private int _index;
		private AdminGlobalAudio adminGlobalAudio = null;

		public void Setup(AdminGlobalAudio admin, string textString, int index)
		{
			myText.text = textString;
			_index = index;
			adminGlobalAudio = admin;
		}

		public void Onclick()
		{
			adminGlobalAudio.PlayAudio(_index); // Gives text to function to play audio.
		}
	}
}
