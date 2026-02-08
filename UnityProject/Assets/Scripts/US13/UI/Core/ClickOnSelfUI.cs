using UnityEngine;
using US13.Player;

namespace US13.UI.Core
{
	public class ClickOnSelfUI : MonoBehaviour
	{
		public static bool SelfClick = false;

		public void ClickOnSelf()
		{
			SelfClick = true;
			PlayerManager.LocalPlayerScript.MouseInputController.CheckClick();
		}
	}
}
