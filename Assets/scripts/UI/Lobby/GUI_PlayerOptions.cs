using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace UI
{
	
public class GUI_PlayerOptions : MonoBehaviour {
		
		private const string UserNamePlayerPref = "NamePickUserName";

		public ControlChat chatNewComponent;

		public InputField idInput;

		public void Start()
		{
			this.chatNewComponent = FindObjectOfType<ControlChat>();


			string prefsName = PlayerPrefs.GetString(GUI_PlayerOptions.UserNamePlayerPref);
			if (!string.IsNullOrEmpty(prefsName))
			{
				this.idInput.text = prefsName;
			}
		}


		// new UI will fire "EndEdit" event also when loosing focus. So check "enter" key and only then StartChat.
		public void EndEditOnEnter()
		{
			if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
			{
				this.StartChat();
			}
		}

		public void StartChat()
		{
			ControlChat chatNewComponent = FindObjectOfType<ControlChat>();
			chatNewComponent.UserName = this.idInput.text.Trim();
			chatNewComponent.Connect();
			enabled = false;

			PlayerPrefs.SetString(GUI_PlayerOptions.UserNamePlayerPref, chatNewComponent.UserName);
		}
}
}
