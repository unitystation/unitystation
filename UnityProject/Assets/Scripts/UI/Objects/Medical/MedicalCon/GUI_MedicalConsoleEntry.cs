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

		public void SetValues(MedicalTerminal.HealthInfo info)
		{
			label.MasterSetValue($"{info.Info}");
		}
	}
}