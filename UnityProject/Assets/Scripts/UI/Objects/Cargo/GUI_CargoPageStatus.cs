using System.Collections;
using UnityEngine;
using UI.Core.NetUI;
using Systems.Cargo;
using Objects.Cargo;


namespace UI.Objects.Cargo
{
	public class GUI_CargoPageStatus : GUI_CargoPage
	{
		[SerializeField]
		private NetText_label logLabel;

		public override void UpdateTab()
		{
			logLabel.SetValue(CargoManager.Instance.CentcomMessage);
		}

		
	}
}
