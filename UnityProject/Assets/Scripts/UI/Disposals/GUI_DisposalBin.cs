using System.Collections;
using UnityEngine;

namespace Disposals
{
	public class GUI_DisposalBin : NetTab
	{
#pragma warning disable 0649
		[SerializeField] NetLabel LabelBinStatus;
		[SerializeField] GUI_ExoFabButton ButtonBinPower;
		[SerializeField] GUI_ExoFabButton ButtonFlushContents;
		[SerializeField] GUI_ExoFabButton ButtonEjectContents;
		[SerializeField] NumberSpinner StoredPressureSpinner;
		[SerializeField] NetColorChanger LEDRed;
		[SerializeField] NetColorChanger LEDYellow;
		[SerializeField] NetColorChanger LEDGreen;
#pragma warning restore 0649

		readonly Color RED_ACTIVE = new Color32(0xFF, 0x1C, 0x00, 0xFF);
		readonly Color RED_INACTIVE = new Color32(0x73, 0x00, 0x00, 0xFF);
		readonly Color YELLOW_ACTIVE = new Color32(0xE4, 0xFF, 0x02, 0xFF);
		readonly Color YELLOW_INACTIVE = new Color32(0x5E, 0x54, 0x00, 0xFF);
		readonly Color GREEN_ACTIVE = new Color32(0x02, 0xFF, 0x23, 0xFF);
		readonly Color GREEN_INACTIVE = new Color32(0x00, 0x5E, 0x00, 0xFF);

		const float UPDATE_RATE = 0.5f;
		Coroutine updateChargeStatus;

		DisposalBin bin;

		#region Initialisation

		void Awake()
		{
			StartCoroutine(WaitForProvider());
		}

		IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}

			bin = Provider.GetComponent<DisposalBin>();

			if (IsServer)
			{
				bin.BinStateUpdated += ServerOnBinStateUpdated;
				ServerOnBinStateUpdated();
			}
		}

		#endregion Initialisation

		void ServerOnBinStateUpdated()
		{
			LabelBinStatus.SetValueServer(bin.BinState.ToString());
			ServerUpdatePressureSpinner();
			ServerSetButtonsAndLEDsByState();
		}

		void ServerUpdatePressureSpinner()
		{
			StoredPressureSpinner.ServerSpinTo(bin.ChargePressure);
		}

		void ServerSetButtonsAndLEDsByState()
		{
			switch (bin.BinState)
			{
				case BinState.Disconnected:
					ServerSetStateDisconnected();
					break;
				case BinState.Off:
					ServerSetStateOff();
					break;
				case BinState.Ready:
					ServerSetStateReady();
					break;
				case BinState.Flushing:
					ServerSetStateFlushing();
					break;
				case BinState.Recharging:
					ServerSetStateRecharging();
					break;
			}
		}

		IEnumerator ServerUpdateChargeStatus()
		{
			while (bin.BinCharging)
			{
				ServerUpdatePressureSpinner();
				LEDYellow.SetValueServer(YELLOW_ACTIVE);
				yield return WaitFor.Seconds(UPDATE_RATE / 2);
				LEDYellow.SetValueServer(YELLOW_INACTIVE);
				yield return WaitFor.Seconds(UPDATE_RATE / 2);
			}
		}

		void ServerEnableButtonInteraction(GUI_ExoFabButton button)
		{
			button.SetValueServer("true");
		}

		void ServerDisableButtonInteraction(GUI_ExoFabButton button)
		{
			button.SetValueServer("false");
		}

		#region State Updates

		void ServerSetStateDisconnected()
		{
			if (updateChargeStatus != null) StopCoroutine(updateChargeStatus);
			ServerDisableButtonInteraction(ButtonBinPower);
			ServerDisableButtonInteraction(ButtonFlushContents);
			ServerEnableButtonInteraction(ButtonEjectContents);
			LEDRed.SetValueServer(RED_INACTIVE);
			LEDYellow.SetValueServer(YELLOW_INACTIVE);
			LEDGreen.SetValueServer(GREEN_INACTIVE);
		}

		void ServerSetStateOff()
		{
			if (updateChargeStatus != null) StopCoroutine(updateChargeStatus);
			ServerEnableButtonInteraction(ButtonBinPower);
			ServerDisableButtonInteraction(ButtonFlushContents);
			ServerEnableButtonInteraction(ButtonEjectContents);
			LEDRed.SetValueServer(RED_INACTIVE);
			LEDYellow.SetValueServer(YELLOW_INACTIVE);
			LEDGreen.SetValueServer(GREEN_INACTIVE);
		}

		void ServerSetStateReady()
		{
			if (updateChargeStatus != null) StopCoroutine(updateChargeStatus);
			ServerEnableButtonInteraction(ButtonBinPower);
			ServerEnableButtonInteraction(ButtonFlushContents);
			ServerEnableButtonInteraction(ButtonEjectContents);
			LEDRed.SetValueServer(RED_INACTIVE);
			LEDYellow.SetValueServer(YELLOW_INACTIVE);
			LEDGreen.SetValueServer(GREEN_ACTIVE);
		}

		void ServerSetStateFlushing()
		{
			if (updateChargeStatus != null) StopCoroutine(updateChargeStatus);
			ServerDisableButtonInteraction(ButtonBinPower);
			ServerDisableButtonInteraction(ButtonFlushContents);
			ServerDisableButtonInteraction(ButtonEjectContents);
			LEDRed.SetValueServer(RED_ACTIVE);
			LEDYellow.SetValueServer(YELLOW_INACTIVE);
			LEDGreen.SetValueServer(GREEN_INACTIVE);
		}

		void ServerSetStateRecharging()
		{
			this.RestartCoroutine(ServerUpdateChargeStatus(), ref updateChargeStatus);
			ServerEnableButtonInteraction(ButtonBinPower);
			ServerDisableButtonInteraction(ButtonFlushContents);
			ServerEnableButtonInteraction(ButtonEjectContents);
			LEDRed.SetValueServer(RED_INACTIVE);
			LEDYellow.SetValueServer(YELLOW_ACTIVE);
			LEDGreen.SetValueServer(GREEN_INACTIVE);
		}

		#endregion State Updates

		#region Buttons

		public void CloseTab()
		{
			ControlTabs.CloseTab(Type, Provider);
		}

		public void ServerTogglePower()
		{
			bin.TogglePower();
		}

		public void ServerFlush()
		{
			bin.FlushContents();
		}

		public void ServerEjectContents()
		{
			bin.EjectContents();
		}

		#endregion Buttons
	}
}
