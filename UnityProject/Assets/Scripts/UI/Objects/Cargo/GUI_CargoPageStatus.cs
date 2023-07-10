using System.Collections;
using UnityEngine;
using UI.Core.NetUI;
using Systems.Cargo;
using Objects.Cargo;


namespace UI.Objects.Cargo
{
	public class GUI_CargoPageStatus : GUI_CargoPage
	{
		private string logs;
		[SerializeField]
		private NetText_label logLabel;

		public override void UpdateTab()
		{
			logs = CargoManager.Instance.CentcomMessage;

			logLabel.SetValue(logs);
		}

		
	}
}
