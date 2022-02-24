using UnityEngine;
using UI.Core.NetUI;
using Objects.Engineering;

namespace UI.Objects.Engineering
{
	public class GUI_ParticleAccelerator : NetTab
	{
		[SerializeField]
		private NetLabel powerSetting = null;

		[SerializeField]
		private NetLabel powerUse = null;

		[SerializeField]
		private NetSlider OnOffSwitch = null;

		private ParticleAcceleratorControl particleAccelerator;
		private ParticleAcceleratorControl ParticleAccelerator =>
				particleAccelerator ??= Provider.GetComponent<ParticleAcceleratorControl>();

		public void OnTabOpenedHandler(ConnectedPlayer connectedPlayer)
		{
			powerSetting.Value = ParticleAccelerator.Status;
			powerUse.Value = ParticleAccelerator.PowerUsage + " volts";
			OnOffSwitch.Value = ((int)(ParticleAccelerator.CurrentState - 3) * 100).ToString();
		}

		public void ClosePanel()
		{
			ControlTabs.CloseTab(Type, Provider);
		}

		public void PowerChange()
		{
			ParticleAccelerator.ChangePower((ParticleAcceleratorState)(int.Parse(OnOffSwitch.Value) / 100 + 3));
			powerSetting.Value = ParticleAccelerator.Status;
			powerUse.Value = ParticleAccelerator.PowerUsage + " volts";
		}

		public void SetUp()
		{
			ParticleAccelerator.ConnectToParts();
			powerSetting.Value = ParticleAccelerator.Status;
			powerUse.Value = ParticleAccelerator.PowerUsage + " volts";
		}
	}
}
