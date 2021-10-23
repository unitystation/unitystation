using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AdminTools
{
	/// <summary>
	/// On the buttons in the list
	/// </summary>
	public class AdminGlobalSoundButton : MonoBehaviour
	{
		public Text myText;
		private int _index;

		private AdminGlobalSound adminGlobalSound;

		public void SetText(string textString)
		{
			myText.text = textString;
		}

		public void SetIndex(int index)
		{
			_index = index;
		}

		public void Onclick()
		{
			adminGlobalSound = GetComponentInParent<AdminGlobalSound>();

			adminGlobalSound.PlaySound(_index); // Gives text to function to play sound.
		}
	}
}
