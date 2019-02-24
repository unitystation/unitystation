using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GeneralSettingsMenu : MonoBehaviour
{
	private bool ttsEnabled = false;
	[SerializeField]
	private Toggle ttsToggle = null;


	/// <summary>
	/// Init is called by UIManager because Start will not be called
	/// on disabled GameObject
	/// </summary>
	public void Init()
	{
		if (PlayerPrefs.HasKey("TTSSetting"))
		{
			ttsEnabled = (PlayerPrefs.GetInt("TTSSetting") == 1);
		}
		else
		{
			ttsEnabled = false;
		}
		ttsToggle.isOn = ttsEnabled;
		UIManager.Instance.ttsToggle = ttsEnabled;
		this.gameObject.SetActive(false);
	}

	public void ToggleTTS()
	{
		ttsEnabled = ttsToggle.isOn;
		UIManager.Instance.ttsToggle = ttsEnabled;
		int ttsValue = ttsEnabled ? 1 : 0;
		PlayerPrefs.SetInt("TTSSetting", ttsValue);
	}
}
