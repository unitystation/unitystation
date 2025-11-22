using System.Linq;
using InGameEvents;
using Objects.Engineering;
using UnityEngine;

namespace InGameEvents
{

	public class EventPlasmaGeneratorTooLoaded : EventScriptBase
	{
		public override void OnEventStart()
		{
			if (FakeEvent) return;

			var Powers = MatrixManager.MainStationMatrix.MatrixMove.GetComponentsInChildren<PowerGenerator>().Where(x => x.IsOn);

			if (Powers.Any() == false) return;

			var Power = Powers.PickRandom();

			var metaDataNode = Power.GetComponentCustom<RegisterTile>().Matrix.GetMetaDataNode(Power.transform.localPosition.RoundToInt());
			foreach (var ElectricalData in metaDataNode.ElectricalData)
			{
				if (ElectricalData.InData.WireEndA == Connection.Overlap || ElectricalData.InData.WireEndB == Connection.Overlap) continue;
				ElectricalData.InData.DestroyThisPlease();
				return;
			}

			base.OnEventStart();
		}
	}
}

