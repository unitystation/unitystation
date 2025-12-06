using System.Linq;
using InGameEvents;
using Items;
using Managers;
using Strings;
using UnityEngine;

namespace InGameEvents
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