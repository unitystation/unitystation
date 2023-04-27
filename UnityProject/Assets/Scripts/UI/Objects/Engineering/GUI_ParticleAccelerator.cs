using UnityEngine;
using UI.Core.NetUI;
using Objects.Engineering;

namespace UI.Objects.Engineering
{
	public class GUI_ParticleAccelerator : NetTab
	{
		[SerializeField]
		private NetText_label powerSetting = null;

		[SerializeField]
		private NetText_label powerUse = null;

		[SerializeField]
		private NetSlider OnOffSwitch = null;

		private ParticleAcceleratorControl particleAccelerator;
		private ParticleAcceleratorControl ParticleAccelerator =>
				particleAccelerator ??= Provider.GetComponent<ParticleAcceleratorControl>();

		public void OnTabOpenedHandler(PlayerInfo connectedPlayer)
		{
			powerSetting.MasterSetValue (ParticleAccelerator.Status);
			powerUse.MasterSetValue ( ParticleAccelerator.PowerUsage + " volts");
			OnOffSwitch.MasterSetValue ( ((int)(ParticleAccelerator.CurrentState - 3) * 100).ToString());
		}

		public void ClosePanel()
		{
			ControlTabs.CloseTab(Type, Provider);
		}

		public void PowerChange()
		{
			ParticleAccelerator.ChangePower((ParticleAcceleratorState)(int.Parse(OnOffSwitch.Value) / 100 + 3));
			powerSetting.MasterSetValue(ParticleAccelerator.Status);
			powerUse.MasterSetValue(ParticleAccelerator.PowerUsage + " volts");
		}

		public void SetUp()
		{
			ParticleAccelerator.ConnectToParts();
			powerSetting.MasterSetValue(ParticleAccelerator.Status);
			powerUse.MasterSetValue( ParticleAccelerator.PowerUsage + " volts");
		}
	}
}
