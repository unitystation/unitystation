using System.Collections;
using UnityEngine;
using Objects.Disposals;
using UI.Core.NetUI;

namespace UI.Objects.Disposals
{
	public class GUI_DisposalBin : NetTab
	{
		[SerializeField] private NetText_label LabelBinStatus = default;
		[SerializeField] private NetInteractiveButton ButtonBinPower = default;
		[SerializeField] private NetInteractiveButton ButtonFlushContents = default;
		[SerializeField] private NetInteractiveButton ButtonEjectContents = default;
		[SerializeField] private NumberSpinner StoredPressureSpinner = default;
		[SerializeField] private NetColorChanger LEDRed = default;
		[SerializeField] private NetColorChanger LEDYellow = default;
		[SerializeField] private NetColorChanger LEDGreen = default;

		private readonly Color RED_ACTIVE = new Color32(0xFF, 0x1C, 0x00, 0xFF);
		private readonly Color RED_INACTIVE = new Color32(0x73, 0x00, 0x00, 0xFF);
		private readonly Color YELLOW_ACTIVE = new Color32(0xE4, 0xFF, 0x02, 0xFF);
		private readonly Color YELLOW_INACTIVE = new Color32(0x5E, 0x54, 0x00, 0xFF);
		private readonly Color GREEN_ACTIVE = new Color32(0x02, 0xFF, 0x23, 0xFF);
		private readonly Color GREEN_INACTIVE = new Color32(0x00, 0x5E, 0x00, 0xFF);

		private readonly float UPDATE_RATE = 0.5f;
		private Coroutine updateChargeStatus;

		private DisposalBin bin;

		#region Initialisation

		private void Awake()
		{
			StartCoroutine(WaitForProvider());
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}

			bin = Provider.GetComponent<DisposalBin>();

			if (IsMasterTab)
			{
				bin.BinStateUpdated += ServerOnBinStateUpdated;
				ServerOnBinStateUpdated();
			}
		}

		#endregion Initialisation

		private void ServerOnBinStateUpdated()
		{
			LabelBinStatus.MasterSetValue(bin.BinState.ToString());
			ServerUpdatePressureSpinner();
			ServerSetButtonsAndLEDsByState();
		}

		private void ServerUpdatePressureSpinner()
		{
			StoredPressureSpinner.ServerSpinTo(Mathf.FloorToInt(bin.ChargePressure));
		}

		private void ServerSetButtonsAndLEDsByState()
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

		private IEnumerator ServerUpdateChargeStatus()
		{
			while (bin.BinCharging)
			{
				ServerUpdatePressureSpinner();
				LEDYellow.MasterSetValue(YELLOW_ACTIVE);
				yield return WaitFor.Seconds(UPDATE_RATE / 2);
				LEDYellow.MasterSetValue(YELLOW_INACTIVE);
				yield return WaitFor.Seconds(UPDATE_RATE / 2);
			}
		}

		private void ServerEnableButtonInteraction(NetInteractiveButton button)
		{
			button.MasterSetValue("true");
		}

		private void ServerDisableButtonInteraction(NetInteractiveButton button)
		{
			button.MasterSetValue("false");
		}

		#region State Updates

		private void ServerSetStateDisconnected()
		{
			if (updateChargeStatus != null)
			{
				StopCoroutine(updateChargeStatus);
			}
			ServerDisableButtonInteraction(ButtonBinPower);
			ServerDisableButtonInteraction(ButtonFlushContents);
			ServerEnableButtonInteraction(ButtonEjectContents);
			LEDRed.MasterSetValue(RED_INACTIVE);
			LEDYellow.MasterSetValue(YELLOW_INACTIVE);
			LEDGreen.MasterSetValue(GREEN_INACTIVE);
		}

		private void ServerSetStateOff()
		{
			if (updateChargeStatus != null)
			{
				StopCoroutine(updateChargeStatus);
			}
			ServerEnableButtonInteraction(ButtonBinPower);
			ServerDisableButtonInteraction(ButtonFlushContents);
			ServerEnableButtonInteraction(ButtonEjectContents);
			LEDRed.MasterSetValue(RED_INACTIVE);
			LEDYellow.MasterSetValue(YELLOW_INACTIVE);
			LEDGreen.MasterSetValue(GREEN_INACTIVE);
		}

		private void ServerSetStateReady()
		{
			if (updateChargeStatus != null)
			{
				StopCoroutine(updateChargeStatus);
			}
			ServerEnableButtonInteraction(ButtonBinPower);
			ServerEnableButtonInteraction(ButtonFlushContents);
			ServerEnableButtonInteraction(ButtonEjectContents);
			LEDRed.MasterSetValue(RED_INACTIVE);
			LEDYellow.MasterSetValue(YELLOW_INACTIVE);
			LEDGreen.MasterSetValue(GREEN_ACTIVE);
		}

		private void ServerSetStateFlushing()
		{
			if (updateChargeStatus != null)
			{
				StopCoroutine(updateChargeStatus);
			}
			ServerDisableButtonInteraction(ButtonBinPower);
			ServerDisableButtonInteraction(ButtonFlushContents);
			ServerDisableButtonInteraction(ButtonEjectContents);
			LEDRed.MasterSetValue(RED_ACTIVE);
			LEDYellow.MasterSetValue(YELLOW_INACTIVE);
			LEDGreen.MasterSetValue(GREEN_INACTIVE);
		}

		private void ServerSetStateRecharging()
		{
			this.RestartCoroutine(ServerUpdateChargeStatus(), ref updateChargeStatus);
			ServerEnableButtonInteraction(ButtonBinPower);
			ServerDisableButtonInteraction(ButtonFlushContents);
			ServerEnableButtonInteraction(ButtonEjectContents);
			LEDRed.MasterSetValue(RED_INACTIVE);
			LEDYellow.MasterSetValue(YELLOW_ACTIVE);
			LEDGreen.MasterSetValue(GREEN_INACTIVE);
		}

		#endregion State Updates

		#region Buttons

		public void ServerTogglePower()
		{
			if (bin.PowerDisconnected)
			{
				foreach (var player in Peepers)
				{
					Chat.AddExamineMsg(player.Mind.Body.gameObject, "This bin is not connected to any power source!");
				}
				return;
			}
			bin.TogglePower();
		}

		public void ServerFlush()
		{
			if (bin.PowerDisconnected || bin.BinState == BinState.Off)
			{
				foreach (var player in Peepers)
				{
					Chat.AddExamineMsg(player.Mind.Body.gameObject, "This bin is not powered!");
				}
				return;
			}
			bin.FlushContents();
		}

		public void ServerEjectContents()
		{
			bin.EjectContents();
		}

		#endregion Buttons
	}
}
