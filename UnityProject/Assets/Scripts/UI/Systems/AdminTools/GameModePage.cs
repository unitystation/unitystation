using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AdminCommands;
using Messages.Client.Admin;


namespace AdminTools
{
	public class GameModePage : AdminPage
	{
		[SerializeField]
		private Text currentText = null;
		[SerializeField]
		private Dropdown nextDropDown = null;
		[SerializeField]
		private Toggle isSecretToggle = null;

		//Next GM change via drop down box
		public void OnNextChange()
		{
			currentData.nextGameMode = nextDropDown.options[nextDropDown.value].text;
			SendEditRequest();
		}

		public void OnSecretChange()
		{
			currentData.isSecret = isSecretToggle.isOn;
			SendEditRequest();
		}

		void SendEditRequest()
		{
			AdminCommandsManager.Instance.CmdChangeGameMode(currentData.nextGameMode, currentData.isSecret);
		}


		public void Send3DRequest()
		{
			AdminCommandsManager.Instance.CmdMake3D();
		}


		public override void OnPageRefresh(AdminPageRefreshData adminPageData)
		{
			base.OnPageRefresh(adminPageData);
			currentText.text = "Current Game Mode: " + adminPageData.currentGameMode;
			isSecretToggle.isOn = adminPageData.isSecret;

			//generate the drop down options:
			var optionData = new List<Dropdown.OptionData>();

			//Add random entry:
			optionData.Add(new Dropdown.OptionData
			{
				text = "Random"
			});

			foreach (var gameMode in adminPageData.availableGameModes)
			{
				optionData.Add(new Dropdown.OptionData
				{
					text = gameMode
				});
			}

			nextDropDown.options = optionData;

			var index = optionData.FindIndex(x => x.text == adminPageData.nextGameMode);
			if (index != -1)
			{
				nextDropDown.value = index;
			}
		}

		public void ToggleOOCMute()
		{
			AdminCommandsManager.Instance.CmdToggleOOCMute();
		}
	}
}
