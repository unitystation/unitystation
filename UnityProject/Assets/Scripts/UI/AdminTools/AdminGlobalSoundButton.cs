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

		private AdminGlobalSound adminGlobalSound;

		public void SetAdminGlobalSoundButtonText(string textString)
		{
			myText.text = textString;
		}

		public void Onclick()
		{
			adminGlobalSound = GetComponentInParent<AdminGlobalSound>();

			adminGlobalSound.PlaySound(myText.text); // Gives text to function to play sound.
		}
	}
}
