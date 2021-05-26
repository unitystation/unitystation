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

		[SerializeField]
		private Toggle memoryProfileToggle = null;

		public void StartProfile()
		{
			AdminCommandsManager.Instance.CmdStartProfile(ServerData.UserID, PlayerList.Instance.AdminToken, (int) framesSlider.value);
		}

		public void StartMemoryProfile()
		{
			AdminCommandsManager.Instance.CmdStartMemoryProfile(ServerData.UserID, PlayerList.Instance.AdminToken, memoryProfileToggle.isOn);
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