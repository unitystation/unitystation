using TMPro;
using UnityEngine;

namespace UI.Systems.ServerInfoPanel
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