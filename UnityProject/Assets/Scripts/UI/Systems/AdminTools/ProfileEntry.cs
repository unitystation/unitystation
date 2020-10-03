using UnityEngine;
using UnityEngine.UI;
using DatabaseAPI;
using AdminCommands;

namespace AdminTools
{
	public class ProfileEntry : MonoBehaviour
	{
		public Text fileName;
		public Text fileSize;

		public void DeleteButton()
		{
			AdminCommandsManager.Instance.CmdDeleteProfile(ServerData.UserID, PlayerList.Instance.AdminToken, fileName.text);
		}

	}
}
