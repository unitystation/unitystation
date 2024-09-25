using System.Collections;
using Objects.Atmospherics;
using UI.Core.NetUI;
using UnityEngine;

namespace UI.Objects.Atmospherics
{
	public class GUI_VolumePump : NetTab
	{
		private VolumePump pump;

		public NetText_label label;
		
		public InputFieldFocus editInputField;
		
		public GameObject editPopup;

		private void Start()
		{
			if (Provider != null)
			{
				pump = Provider.GetComponentInChildren<VolumePump>();
			}
			label.MasterSetValue(pump.TransferVolume.ToString("000"));
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
				pump.TransferVolume = Mathf.Clamp(input, 0, pump.MaxVolume);
				label.MasterSetValue(pump.TransferVolume.ToString("000"));
			}
		}
	}	
}