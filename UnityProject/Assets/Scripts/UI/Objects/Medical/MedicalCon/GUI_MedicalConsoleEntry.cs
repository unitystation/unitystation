using Items;
using Objects.Medical;
using UI.Core.NetUI;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Objects.Medical.MedicalCon
{
	public class GUI_MedicalConsoleEntry : DynamicEntry
	{
		[SerializeField] private NetText_label label;
		[SerializeField] private Image image;
		[SerializeField] private Color unknownColor;
		[SerializeField] private Color healthyColor;
		[SerializeField] private Color dangerColor;
		[SerializeField] private Color deadColor;

		public void SetValues(MedicalTerminal.HealthInfo info)
		{
			label.MasterSetValue($"{info.Info}");
			if (info.Mode == SuitSensor.SensorMode.OFF)
			{
				image.color = unknownColor;
				return;
			}

			if (info.HealthPercent >= 75)
			{
				image.color = healthyColor;
			}
			if (info.HealthPercent < 75)
			{
				image.color = dangerColor;
			}
			if (info.HealthPercent <= 25)
			{
				image.color = deadColor;
			}
		}
	}
}