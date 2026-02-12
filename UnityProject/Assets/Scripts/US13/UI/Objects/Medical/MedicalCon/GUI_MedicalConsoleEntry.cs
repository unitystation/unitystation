using UnityEngine;
using US13.Objects.Medical;
using US13.UI.Core.Net.Elements;
using US13.UI.Core.Net.Elements.Dynamic;

namespace US13.UI.Objects.Medical.MedicalCon
{
	public class GUI_MedicalConsoleEntry : DynamicEntry
	{
		[SerializeField] private NetText_label label;

		public void SetValues(MedicalTerminal.HealthInfo info)
		{
			label.MasterSetValue($"{info.Info}");
		}
	}
}