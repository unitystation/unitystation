using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AdminGlobalSoundButton : MonoBehaviour
{
	public Text myText;

	public int index;

	private AdminGlobalSound adminGlobalSound;

	public void SetAdminGlobalSoundButtonText(string textString)
	{
		myText.text = textString;
	}

	public void Onclick()
	{
		adminGlobalSound = GetComponentInParent<AdminGlobalSound>();

		adminGlobalSound.PlaySound(index);// Gives index to GhostTeleport.cs

	}
}
