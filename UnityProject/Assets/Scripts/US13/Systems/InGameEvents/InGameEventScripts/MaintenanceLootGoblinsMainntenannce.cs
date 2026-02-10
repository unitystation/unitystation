using System.Linq;
using US13.Items.Others;
using US13.Managers;
using US13.Managers.MatrixManager;
using US13.Strings;
using Util;

namespace US13.Systems.InGameEvents.InGameEventScripts
{
	public class MaintenanceLootGoblinsMainntenannce : EventScriptBase
	{
		public override void OnEventStart()
		{
			var Spawners = MatrixManager.MainStationMatrix.Matrix.MetaDataLayer.EtherealThings.Where(x =>
				x.GetComponentCustom<RandomItemSpot>());

			if (Spawners.Any() == false) return;

			var text = "Incoming Central Command Goblin Detection:\n We've detected a surge of maintenance goblin activity in your maintenance.";

			CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, text, CentComm.UpdateSound.Alert);

			foreach (var Spawner in Spawners)
			{
				Spawner.GetComponentCustom<RandomItemSpot>().RollRandomPool(true, true);
			}
		}
	}
}