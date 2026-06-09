using UnityEngine;
using US13.Player;

namespace US13.UI.Systems.AdminTools
{
	public class AGhost : MonoBehaviour
	{
		public void OnClick()
		{
			Ghost();
		}

		public static void Ghost()
		{
			if (PlayerManager.LocalPlayerScript == null) return;

			PlayerManager.LocalMindScript.CmdAGhost();
		}
	}
}
