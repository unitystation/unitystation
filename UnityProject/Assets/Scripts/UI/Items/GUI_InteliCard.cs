using UnityEngine;
using UI.Core.NetUI;
using Objects.Research;

namespace UI.Items
{
	public class GUI_InteliCard : NetTab
	{
		[SerializeField]
		private NetText_label labelLaws = null;

		[SerializeField]
		private NetText_label labelPurgeButton = null;

		[SerializeField]
		private NetSlider allowRemoteActionsSlider = null;

		[SerializeField]
		private NetSlider allowRadioSlider = null;

		[SerializeField]
		private NetSlider integritySlider = null;

		private AiVessel aiVessel;
		private AiVessel AiVessel => aiVessel ??= Provider.GetComponent<AiVessel>();

		public void OnTabOpenedHandler(PlayerInfo connectedPlayer)
		{
			allowRemoteActionsSlider.MasterSetValue(AiVessel.AllowRemoteAction ? (1 * 100).ToString() : "0");
			allowRadioSlider.MasterSetValue(AiVessel.AllowRadio ? (1 * 100).ToString() : "0");

			if (AiVessel.LinkedPlayer == null)
			{
				labelLaws.MasterSetValue("This intelicard holds no Ai");
				integritySlider.MasterSetValue( "0");
				labelPurgeButton.MasterSetValue( "No Ai to Purge"); ;
				return;
			}

			labelPurgeButton.MasterSetValue(AiVessel.LinkedPlayer.IsPurging ? "Stop Purging" : "Start Purging");
			labelLaws.MasterSetValue(AiVessel.LinkedPlayer.GetLawsString());
			integritySlider.MasterSetValue(AiVessel.LinkedPlayer.Integrity.ToString());
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
