using System.Collections;
using System.Collections.Generic;
using SecureStuff;
using UnityEngine;
using UI.Chat_UI;

namespace UI
{
	public class MiniActionsHelpWindowUI : WindowDrag // TODO: Interesting inheritance?
	{
		[SerializeField] private string wikiURL = "https://unitystation.github.io/unitystation-wiki/";

		public void OnClickExit()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);

			gameObject.SetActive(false);
		}

		/// <summary>
		/// A) Report something to the Admins
		/// </summary>
		public void OnClickOption1()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);

			ChatUI.Instance.OnAdminHelpButton();
		}

		/// <summary>
		/// B) Request the help of a Mentor
		/// </summary>
		public void OnClickOption2()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
		}

		/// <summary>
		/// C) Open the Wiki in your browser
		/// </summary>
		public void OnClickOption3()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);

			SafeURL.Open(wikiURL);
		}
	}
}
