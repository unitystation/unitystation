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
			ServerCommandVersionTwoMessageClient.Send(ServerData.UserID, PlayerList.Instance.AdminToken, fileName.text, "CmdDeleteProfile");
		}

	}
}
