using System;
using UnityEngine;
using US13.UI.Core.Net.Elements;
using US13.UI.Core.Net.Page;

namespace US13.UI.Objects.Atmospherics.ACU.Pages
{
	/// <summary>
	/// A modal overlay for the <see cref="GUI_Acu"/> which allows the peeper to type in a value.
	/// </summary>
	public class GUI_AcuValueModal : GUI_AcuPage
	{
		[SerializeField]
		private NetPageSwitcher modalSwitcher = default;

		[SerializeField]
		private NetText_label placeholderLabel = default;
		[SerializeField]
		private NetTMPSubmitButton submitButton = default;

		private Action<string> latestCaller;

		/// <summary>
		/// Opens the modal, displaying the overlay.
		/// </summary>
		/// <param name="value">The initial value the modal should open with</param>
		/// <param name="callback">The action to be invoked when the peeper submits the new value.</param>
		public void Open(string value, Action<string> callback)
		{
			latestCaller = callback;
			placeholderLabel.MasterSetValue(value);
			modalSwitcher.SetActivePage(this);
		}

		/// <summary>
		/// Close the modal without submitting the new value.
		/// </summary>
		public void Close()
		{
			modalSwitcher.SetActivePage(0);
		}

		#region Buttons

		public void BtnOk()
		{
			AcuUi.PlayTap();
			latestCaller.Invoke(submitButton.Value);

			modalSwitcher.SetActivePage(0);
		}

		public void BtnCancel()
		{
			AcuUi.PlayTap();
			Close();
		}

		#endregion
	}
}
