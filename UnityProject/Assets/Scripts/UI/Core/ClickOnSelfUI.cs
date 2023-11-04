using UnityEngine;

namespace UI.Core
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
