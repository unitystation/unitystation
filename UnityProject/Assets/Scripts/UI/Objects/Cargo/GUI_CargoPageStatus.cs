using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Systems.Cargo;

namespace UI.Objects.Cargo
{
	public class GUI_CargoPageStatus : GUI_CargoPage
	{
		public NetText_label creditsText;
		public NetText_label shuttleButtonText;
		public NetText_label messageText;
		public NetColorChanger statusCargoImage;
		public NetColorChanger statusTransitImage;
		public NetColorChanger statusCentcomImage;

		public override void OpenTab()
		{
			CargoManager.Instance.OnCreditsUpdate.AddListener(UpdateTab);
			CargoManager.Instance.OnShuttleUpdate.AddListener(UpdateTab);
			CargoManager.Instance.OnTimerUpdate.AddListener(UpdateTab);
		}

		public override void UpdateTab()
		{
			var cm = CargoManager.Instance;

			if (cm.ShuttleStatus == ShuttleStatus.OnRouteCentcom ||
				cm.ShuttleStatus == ShuttleStatus.OnRouteStation)
			{
				if (cm.CurrentFlyTime > 0)
				{
					var min = Mathf.FloorToInt(cm.CurrentFlyTime / 60).ToString();
					var sec = (cm.CurrentFlyTime % 60).ToString();
					sec = sec.Length >= 10 ? sec : $"0{sec}";

					shuttleButtonText.SetValueServer($"{min}:{sec}");
				}
				else
				{
					shuttleButtonText.SetValueServer("ARRIVING");
				}
				SetShuttleStatus(statusTransitImage);
			}
			else
			{
				SetShuttleStatus(cm.ShuttleStatus == ShuttleStatus.DockedStation ? statusCargoImage : statusCentcomImage);
				shuttleButtonText.SetValueServer("SEND");
			}

			messageText.SetValueServer(cm.CentcomMessage);
			creditsText.SetValueServer(cm.Credits.ToString());
		}

		//Current shuttle status is displayed like a switch - only one is active
		private void SetShuttleStatus(NetColorChanger objToSwitch)
		{
			statusCargoImage.SetValueServer(Color.black);
			statusTransitImage.SetValueServer(Color.black);
			statusCentcomImage.SetValueServer(Color.black);

			objToSwitch.SetValueServer(Color.white);
		}
	}
}
