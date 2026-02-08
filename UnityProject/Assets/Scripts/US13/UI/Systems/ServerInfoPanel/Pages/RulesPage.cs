using TMPro;
using UnityEngine;

namespace US13.UI.Systems.ServerInfoPanel.Pages
{
	public class RulesPage: InfoPanelPage
	{
		[SerializeField] private TMP_Text rulesText;


		public void PopulatePage(string rulesContent)
		{
			rulesText.text = rulesContent;
		}

		public override bool HasContent()
		{
			return string.IsNullOrEmpty(rulesText.text) == false;
		}
	}
}