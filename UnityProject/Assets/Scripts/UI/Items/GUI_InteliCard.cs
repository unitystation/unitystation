using Objects.Research;
using UnityEngine;

namespace UI.Items
{
	public class GUI_InteliCard : NetTab
	{
		[SerializeField]
		private NetLabel labelLaws = null;

		[SerializeField]
		private NetLabel labelPurgeButton = null;

		[SerializeField]
		private NetSlider allowRemoteActionsSlider = null;

		[SerializeField]
		private NetSlider allowRadioSlider = null;

		[SerializeField]
		private NetSlider integritySlider = null;

		private AiVessel aiVessel;
		private AiVessel AiVessel {
			get {
				if (aiVessel == null)
					aiVessel = Provider.GetComponent<AiVessel>();

				return aiVessel;
			}
		}

		public void OnTabOpenedHandler(ConnectedPlayer connectedPlayer)
		{
			allowRemoteActionsSlider.SetValueServer(AiVessel.AllowRemoteAction ? (1 * 100).ToString() : "0");
			allowRadioSlider.SetValueServer(AiVessel.AllowRadio ? (1 * 100).ToString() : "0");

			if (AiVessel.LinkedPlayer == null)
			{
				labelLaws.Value = "This intelicard holds no Ai";
				integritySlider.Value = "0";
				labelPurgeButton.Value = "No Ai to Purge";
				return;
			}

			labelPurgeButton.Value = AiVessel.LinkedPlayer.IsPurging ? "Stop Purging" : "Start Purging";
			labelLaws.Value = AiVessel.LinkedPlayer.GetLawsString();
			integritySlider.Value = AiVessel.LinkedPlayer.Integrity.ToString();
		}

		public void OnRemoveActionChange()
		{
			//Try get On/Off switch value
			var onValue = int.Parse(allowRemoteActionsSlider.Value) / 100;
			if(onValue == 0 || onValue == 1)
			{
				AiVessel.ChangeRemoteActionState(onValue != 0);
			}
		}

		public void OnRadioChange()
		{
			//Try get On/Off switch value
			var onValue = int.Parse(allowRadioSlider.Value) / 100;
			if(onValue == 0 || onValue == 1)
			{
				AiVessel.ChangeRadioState(onValue != 0);
			}
		}

		public void OnPurgeButtonPress()
		{
			if(AiVessel.LinkedPlayer == null) return;

			AiVessel.LinkedPlayer.SetPurging(!AiVessel.LinkedPlayer.IsPurging);
		}
	}
}
