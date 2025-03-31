using System.Collections.Generic;
using System.Text;
using Objects.Machines;
using UI.Core.NetUI;
using UnityEngine;

namespace UI.Objects
{
	public class GUI_FlatpackerMachinePage : NetPage
	{
		[SerializeField] private NetText_label machineNameLabel = null;
		[SerializeField] private NetText_label materialLabelA = null;
		[SerializeField] private NetText_label materiallabelB = null;

		private GUI_Flatpacker master = null;

		public void Fabricate()
		{
			if (master.provider == null || master.hasFunds == false) return;
			master.provider.BeginProduction();
			master.CloseTab();
		}

		public void EjectMachineBoard()
		{
			if (master.provider == null) return;
			master.provider.EjectMachineBoard();
			master.CloseTab();
		}

		public void SetMaster(GUI_Flatpacker master)
		{
			this.master = master;
		}

		public void UpdateText(string machineName, Dictionary<MaterialSheet, int> neededMats, Dictionary<ItemTrait, int> currentMats, ref bool hasFunds)
		{
			machineNameLabel.MasterSetValue(machineName);
			materiallabelB.MasterSetValue("");
			NetText_label target = materialLabelA;

			StringBuilder sb = new StringBuilder();
			int lines = 0;
			foreach (var material in neededMats)
			{
				if (++lines > 3)
				{
					target.MasterSetValue(sb.ToString());
					sb.Clear();
					target = materiallabelB;
				}

				if (currentMats.ContainsKey(material.Key.materialTrait) == false ||
				    currentMats[material.Key.materialTrait] < material.Value)
				{
					hasFunds = false;
					sb.AppendLine($"<color=#FF0000>{material.Key.displayName} - {material.Value}</color>");
				}
				else sb.AppendLine($"{material.Key.displayName} - {material.Value}");
			}
			target.MasterSetValue(sb.ToString());
		}
	}
}
