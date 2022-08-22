using System;
using System.Collections;
using UnityEngine;
using UI.Core.NetUI;

namespace UI.Items.PDA
{
	public class GUI_PDASettingMenu : NetPage, IPageReadyable
	{
		[SerializeField] private GUI_PDA controller = null;

		[SerializeField] public NetText_label input;

		private bool clickedRecently; // a simple variable to make sure the PDA asks the player to confirm the reset

		public void OnPageActivated()
		{
			controller.SetBreadcrumb("/bin/settings.sh");
		}

		public void SetRingtone(string ringtone)
		{
			if (string.IsNullOrEmpty(ringtone)) return;

			if (controller.PDA.IsUplinkCapable)
			{
				controller.PDA.UnlockUplink(ringtone);
			}

			if (!controller.PDA.IsUplinkLocked)
			{
				controller.OpenPage(controller.uplinkPage);
			}
			else
			{
				controller.PDA.SetRingtone(ringtone);
				controller.PDA.PlayRingtone();
			}

			input.SetValueServer("");
		}

		public void EjectCartridge()
		{
			controller.PDA.EjectCartridge();
		}

		/// <summary>
		/// Tells the PDA to unregister the name and tell the messenger that it is "unknown"
		/// </summary>
		public void FactoryReset()
		{
			if (clickedRecently)
			{
				clickedRecently = false;
				controller.PDA.ResetPDA();
			}
			else
			{
				StartCoroutine(ResetTimer());
				clickedRecently = true;
			}
		}

		// Unimplemented
		public void Themes()
		{
			controller.PlayDenyTone();
		}

		private IEnumerator ResetTimer()
		{
			yield return WaitFor.Seconds(0.5f);
			clickedRecently = false;
		}
	}
}
