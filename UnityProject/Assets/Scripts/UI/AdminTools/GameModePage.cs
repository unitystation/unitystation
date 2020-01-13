using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AdminTools
{
	public class GameModePage : AdminPage
	{
		[SerializeField]
		private Text currentText;
		[SerializeField]
		private Dropdown nextDropDown;
		[SerializeField]
		private Toggle isSecretToggle;

		//Next GM change via drop down box
		public void OnNextChange()
		{
			
		}

		public void OnSecretChange()
		{

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
	}
}