using UnityEngine;
using UnityEngine.UI;
using PlayGroup;

namespace UI
{
	/// <summary>
	///		This button is used to let a player switch keyboard input method
	///		from QWERTY to AZERTY, and viceversa. The button text will say
	///		what mode it is currently in.
	/// </summary>
	public class ControlKeyboard : MonoBehaviour
	{
		private Button button;

		private void OnEnable()
		{
			if (!button)
			{
				button = GetComponent<Button>();
			}
			button.GetComponentInChildren<Text>().text = "QWERTY";
		}


		public void ChangeKeyboardInput()
		{
			if (PlayerManager.LocalPlayerScript)
			{
				PlayerMove plm = PlayerManager.LocalPlayerScript.playerMove;
				if(button.GetComponentInChildren<Text>().text == "QWERTY")
				{
					plm.ChangeKeyboardInput(true);
					button.GetComponentInChildren<Text>().text = "AZERTY";
				}
				else
				{
					plm.ChangeKeyboardInput(false);
					button.GetComponentInChildren<Text>().text = "QWERTY";
				}
			}
		}
	}
}
