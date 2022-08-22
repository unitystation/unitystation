using AdminCommands;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace AdminTools
{
	public class ServerSettingsPage : AdminPage
	{
		[FormerlySerializedAs("inputField")] [SerializeField]
		private TMP_InputField playerLimitInputField = null;

		[SerializeField]
		private TMP_InputField serverMaxFrameRate = null;

		public override void OnPageRefresh(AdminPageRefreshData adminPageData)
    	{
    		base.OnPageRefresh(adminPageData);
            playerLimitInputField.text = adminPageData.playerLimit.ToString();
            serverMaxFrameRate.text = adminPageData.maxFrameRate.ToString();
        }

		public void OnChangePlayerLimit()
		{
			if (int.TryParse(playerLimitInputField.text, out var value))
			{
				if (value < 0)
				{
					value = 0;
				}

				AdminCommandsManager.Instance.CmdChangePlayerLimit(value);
			}
		}

		public void OnChangeFrameRate()
		{
			if (int.TryParse(serverMaxFrameRate.text, out var value))
			{
				if (value < AdminCommandsManager.MINIUM_SERVER_FRAMERATE)
				{
					value = AdminCommandsManager.MINIUM_SERVER_FRAMERATE;
				}

				AdminCommandsManager.Instance.CmdChangeFrameRate(value);
			}
		}
	}
}
