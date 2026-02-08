using TMPro;
using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Core.Input_System;
using US13.Messages.Client;

namespace US13.UI.Systems.MainHUD.UI_Bottom
{
	public class VOXText : MonoBehaviour
	{
		public TMP_Text Text;
		public bool IsSearch = false;

		public VOXUI VOXUI;

		public AddressableAudioSource AddressableAudioSource;

		public void SetUp(string Text, VOXUI VOXUI, bool Search)
		{
			this.Text.text = Text;
			IsSearch = Search;
			this.VOXUI = VOXUI;
		}

		public void OnPress()
		{
			if (IsSearch)
			{
				VOXUI.AddToSaysBox(this);
			}
			else
			{
				if (KeyboardInputManager.IsShiftPressed())
				{
					VOXUI.RemoveAndDestroyFromSaysBox(this);
				}
				else
				{
					RequestVOXsay.Send(this.Text.text);
				}
			}
		}
	}
}