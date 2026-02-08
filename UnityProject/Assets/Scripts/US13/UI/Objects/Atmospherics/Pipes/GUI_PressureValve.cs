using System.Collections;
using UnityEngine;
using US13.Objects.Pipes.Devices;
using US13.UI.Core;
using US13.UI.Core.Net;
using US13.UI.Core.Net.Elements;
using US13.UI.Systems;

namespace US13.UI.Objects.Atmospherics.Pipes
{
	public class GUI_PressureValve : NetTab
	{
		private PressureValve Pump;

		public NetText_label label;

		public InputFieldFocus editInputField;

		public GameObject editPopup;

		private void Start()
		{
			if (Provider != null)
			{
				Pump = Provider.GetComponentInChildren<PressureValve>();
			}
			label.MasterSetValue(Pump.TargetPressure.ToString("0000.00"));
			editPopup.SetActive(false);
		}

		public void OpenPopup()
		{
			editPopup.SetActive(true);
			editInputField.text = label.Value;
			editInputField.Select();
		}

		public void ClosePopup()
		{
			editPopup.SetActive(false);
			StartCoroutine(WaitToEnableInput());
		}

		private IEnumerator WaitToEnableInput()
		{
			yield return WaitFor.EndOfFrame;
			UIManager.IsInputFocus = false;
			UIManager.PreventChatInput = false;
		}

		public void ServerSetReleasePressure(string newValue)
		{
			if (string.IsNullOrEmpty(newValue)) return;
			if (float.TryParse(newValue, out var input))
			{
				Pump.TargetPressure = Mathf.Clamp(input, 0, Pump.MaxPressure);
				label.MasterSetValue(Pump.TargetPressure.ToString("0000.00"));
			}
		}
	}
}