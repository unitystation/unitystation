using UnityEngine;
using UnityEngine.UI;
using DatabaseAPI;
using AdminCommands;

namespace AdminTools
{
	public class DevToolsPage : MonoBehaviour
	{
		public Slider framesSlider;
		public InputField framesInput;

		public void StartProfile()
		{
			ServerCommandVersionSixMessageClient.Send(ServerData.UserID, PlayerList.Instance.AdminToken, (int) framesSlider.value, "CmdStartProfile");
		}

		public void ChangeInputField()
		{
			if (int.TryParse(framesInput.text, out var value))
			{
				framesSlider.value = value;
			}
			else
			{
				ChangeSlider();
			}
		}

		public void ChangeSlider()
		{
			framesInput.text = framesSlider.value.ToString();
		}
	}
}