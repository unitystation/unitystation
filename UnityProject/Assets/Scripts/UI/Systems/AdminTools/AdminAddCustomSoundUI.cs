using System;
using AdminCommands;
using Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Systems.AdminTools
{
	public class AdminAddCustomSoundUI : MonoBehaviour
	{
		[SerializeField] private TMP_Text errorText;
		[SerializeField] private TMP_InputField linkText;
		[SerializeField] private TMP_InputField fileNameText;
		[SerializeField] private TMP_InputField idText;
		[SerializeField] private Toggle isMusicToggle;
		[SerializeField] private Toggle isLobbyToggle;

		private void OnEnable()
		{
			errorText.text = "";
		}

		public void OnClick()
		{
			var parsedInt = int.Parse(idText.text);
			if (SimpleAudioManager.Instance.SharedData.ContainsKey(parsedInt))
			{
				errorText.text = "Id already exists!";
				return;
			}

			SimpleAudioManager.SimpleAudioData newData = new SimpleAudioManager.SimpleAudioData();
			newData.FileTitle = fileNameText.text;
			newData.LinkToFile = linkText.text;
			newData.ID = parsedInt;
			newData.IsMusic = isMusicToggle.isOn;
			newData.PlaysInLobby = isLobbyToggle.isOn;
			AdminCommandsManager.Instance.CmdAddNewSoundToSimpleSoundList(newData);
			this.SetActive(false);
		}
	}
}