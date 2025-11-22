using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Systems.Electricity.Inheritance;

namespace Systems.Electricity.NodeModules
{
	public class TransformerModule : ElectricalModuleInheritance
	{
		[Header("Transformer Settings")]
		public float TurnRatio; // the Turn ratio of the transformer so if it 2, 1v in 2v out
		public bool InvertingTurnRatio;  // what it will be limited to
		public float VoltageLimiting; // If it requires VoltageLimiting and  At what point the VoltageLimiting will kick in
		public float VoltageLimitedTo;  // what it will be limited to

		public List<PowerTypeCategory> HighsideConnections = new List<PowerTypeCategory>();
		public List<PowerTypeCategory> LowsideConnections = new List<PowerTypeCategory>();
		public PowerTypeCategory TreatComingFromNullAs;

		public void BroadcastSetUpMessage(ElectricalNodeControl node)
		{
			RequiresUpdateOn = new HashSet<ElectricalUpdateTypeCategory>
			{
				ElectricalUpdateTypeCategory.ModifyElectricityInput,
				ElectricalUpdateTypeCategory.ModifyResistancyOutput,
			};
			ModuleType = ElectricalModuleTypeCategory.Transformer;
			ControllingNode = node;
			if (InvertingTurnRatio) {
				TurnRatio = 1 / TurnRatio;
			}
			node.AddModule(this);
		}

		public override VIRCurrent ModifyElectricityInput(VIRCurrent Current,
			ElectricalOIinheritance SourceInstance,
			IntrinsicElectronicData ComingFromm)
		{
			float Resistance = ElectricityFunctions.WorkOutResistance(ControllingNode.Node.InData.Data.SupplyDependent[SourceInstance].ResistanceGoingTo);
			bool HiSide = HighsideConnections.Contains(ComingFromm.Categorytype);
			var Voltage = 0f;
			if (VoltageLimiting != 0 && HiSide)
			{
				var LowSides = ControllingNode.Node.InData.Data
					.SupplyDependent[SourceInstance].ResistanceComingFrom;

				if (LowSides.Count > 0)
				{
					var LowSide = LowSides.First().Key;
					ElectricityFunctions.WorkOutActualNumbers(LowSide);
					Voltage =  LowSide.Data.ActualVoltage;
				}
			}



			VIRCurrent Currentout =
				TransformerCalculations.ElectricalStageTransformerCalculate(this,
					Current,
					Resistance,
					Voltage,
					HiSide);
			return Currentout;
		}

		public override ResistanceWrap ModifyResistancyOutput(ResistanceWrap Resistance, ElectricalOIinheritance SourceInstance)
		{
			//return (Resistance);
			bool FromHighSide = false;
			foreach (var Upst in ControllingNode.Node.InData.Data.SupplyDependent[SourceInstance].Upstream) {
				if (LowsideConnections.Contains(Upst.Categorytype)) {
					FromHighSide = true;
				}
			}


			ResistanceWrap ResistanceM = TransformerCalculations.ResistanceStageTransformerCalculate(this, ResistanceToModify: Resistance, FromHighSide : FromHighSide);
			return (ResistanceM);
		}
	}
}
