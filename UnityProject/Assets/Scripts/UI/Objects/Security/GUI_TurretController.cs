using UnityEngine;
using UI.Core.NetUI;
using Objects.Wallmounts.Switches;

namespace UI.Objects.Security
{
	public class GUI_TurretController : NetTab
	{
		[SerializeField]
		private NetText_label powerSetting = null;

		[SerializeField]
		private NetSlider onOffSwitch = null;

		[SerializeField]
		private NetSlider stunLethalSwitch = null;

		private TurretSwitch turretSwitch;
		private TurretSwitch TurretSwitch => turretSwitch ??= Provider.GetComponent<TurretSwitch>();

		public void OnTabOpenedHandler(PlayerInfo connectedPlayer)
		{
			ChangeStatus();

			onOffSwitch.MasterSetValue(TurretSwitch.IsOn ? (1 * 100).ToString() : "0");
			stunLethalSwitch.MasterSetValue(TurretSwitch.IsStun ? "0" : (1 * 100).ToString());
		}

		private void ChangeStatus()
		{
			if (TurretSwitch.HasPower == false)
			{
				powerSetting.MasterSetValue("No Power");
			}
			else
			{
				powerSetting.MasterSetValue(TurretSwitch.IsOn ? TurretSwitch.IsStun ? "Stun" : "Lethal" : "Off");
			}
		}

		public void OnIsOnChange()
		{
			//Try get On/Off switch value
			var onValue = int.Parse(onOffSwitch.Value) / 100;
			if (onValue == 0 || onValue == 1)
			{
				TurretSwitch.ChangeOnState(onValue != 0);
			}

			ChangeStatus();
		}

		public void OnIsStunChange()
		{
			//Try get Stun/Lethal Value
			var stunValue = int.Parse(stunLethalSwitch.Value) / 100;
			if (stunValue == 0 || stunValue == 1)
			{
				TurretSwitch.ChangeStunState(stunValue == 0);
			}

			ChangeStatus();
		}
	}
}
