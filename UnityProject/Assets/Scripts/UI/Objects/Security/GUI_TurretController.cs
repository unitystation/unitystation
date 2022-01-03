using Objects.Wallmounts.Switches;
using UnityEngine;

namespace UI.Objects.Security
{
	public class GUI_TurretController : NetTab
	{
		[SerializeField]
		private NetLabel powerSetting = null;

		[SerializeField]
		private NetSlider onOffSwitch = null;

		[SerializeField]
		private NetSlider stunLethalSwitch = null;

		private TurretSwitch turretSwitch;
		private TurretSwitch TurretSwitch {
			get {
				if (turretSwitch == null)
					turretSwitch = Provider.GetComponent<TurretSwitch>();

				return turretSwitch;
			}
		}

		public void OnTabOpenedHandler(ConnectedPlayer connectedPlayer)
		{
			ChangeStatus();

			onOffSwitch.Value = TurretSwitch.IsOn ? (1 * 100).ToString() : "0";
			stunLethalSwitch.Value = TurretSwitch.IsStun ? "0" : (1 * 100).ToString();
		}

		private void ChangeStatus()
		{
			if (TurretSwitch.HasPower == false)
			{
				powerSetting.Value = "No Power";
			}
			else
			{
				powerSetting.Value = TurretSwitch.IsOn ? TurretSwitch.IsStun ? "Stun" : "Lethal" : "Off";
			}
		}

		public void OnIsOnChange()
		{
			//Try get On/Off switch value
			var onValue = int.Parse(onOffSwitch.Value) / 100;
			if(onValue == 0 || onValue == 1)
			{
				TurretSwitch.ChangeOnState(onValue != 0);
			}

			ChangeStatus();
		}

		public void OnIsStunChange()
		{
			//Try get Stun/Lethal Value
			var stunValue = int.Parse(stunLethalSwitch.Value) / 100;
			if(stunValue == 0 || stunValue == 1)
			{
				TurretSwitch.ChangeStunState(stunValue == 0);
			}

			ChangeStatus();
		}
	}
}
