using AdminCommands;
using TMPro;
using UnityEngine;

namespace AdminTools
{
	public class ServerSettingsPage : AdminPage
	{
		[SerializeField]
		private TMP_InputField inputField = null;

		public override void OnPageRefresh(AdminPageRefreshData adminPageData)
    	{
    		base.OnPageRefresh(adminPageData);
            inputField.text = adminPageData.playerLimit.ToString();
        }

		public void OnChangeInputField()
		{
			if (int.TryParse(inputField.text, out var value))
			{
				if (value < 0) 
				{
					value = 0;
				}
				
				AdminCommandsManager.Instance.CmdChangePlayerLimit(value);
			}
		}
	}
}
