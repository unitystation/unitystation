using UnityEngine;
using UnityEngine.UI;
using AdminCommands;


namespace AdminTools
{
	public class ProfileEntry : MonoBehaviour
	{
		public Text fileName;
		public Text fileSize;

		public void DeleteButton()
		{
			AdminCommandsManager.Instance.CmdDeleteProfile(fileName.text);
		}

	}
}
