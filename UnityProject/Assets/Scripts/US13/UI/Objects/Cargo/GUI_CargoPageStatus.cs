using UnityEngine;
using US13.Managers;
using US13.UI.Core.Net.Elements;

namespace US13.UI.Objects.Cargo
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
