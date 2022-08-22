using UnityEngine;
using UnityEngine.UI;
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
			AdminCommandsManager.Instance.CmdStartProfile((int) framesSlider.value);
		}

		public void StartMemoryProfile()
		{
			AdminCommandsManager.Instance.CmdStartMemoryProfile(memoryProfileToggle.isOn);
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
