using UnityEngine;
using UnityEngine.UI;
using US13.Managers;

namespace US13.UI.Systems.AdminTools
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
