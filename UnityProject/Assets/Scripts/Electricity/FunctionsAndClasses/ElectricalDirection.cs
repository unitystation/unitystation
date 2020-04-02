using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ElectricalDirections
{	public List<ElectricalDirectionStep> LiveSteps = new List<ElectricalDirectionStep>();
	public ElectricalNodeControl RelatedNode;
	public ElectricalDirectionStep StartingStep;

	//public void StartSearch(ElectricalNodeControl NodeControl)
	//{
	//	RelatedNode = NodeControl;
	//	StartingStep = ElectricalSynchronisation.GetStep();
	//	LiveSteps.Add(StartingStep);
	//	StartingStep.InData = NodeControl.Node.InData;
	//	StartingStep.Jump(this, null, new HashSet<IntrinsicElectronicData>());
	//}


	public void Pool()
	{
		ElectricalSynchronisation.PooledSteps.AddRange(LiveSteps);
	}
}


public class ElectricalDirectionStep
{

	//public static HashSet<PowerTypeCategory> CompatibleCables = new HashSet<PowerTypeCategory> {
	//	PowerTypeCategory.StandardCable,
	//	PowerTypeCategory.LowVoltageCable,
	//	PowerTypeCategory.HighVoltageCable
	//};
	//public HashSet<ElectricalDirectionStep> Downstream = new HashSet<ElectricalDirectionStep>();
	//public ElectricalDirectionStep Upstream;
	//public IntrinsicElectronicData InData;
	//public HashSet<Resistance> Sources = new HashSet<Resistance>();
	//public VIRResistances resistance = ElectricalPool.GetVIRResistances();

	//public void Clean()
	//{
	//	Downstream.Clear();
	//	resistance.ResistanceSources.Clear();
	//	Sources.Clear();
	//	InData = null;
	//}


	//public void Jump(ElectricalDirections MasterClass,
	//				 ElectricalDirectionStep PreviousStep,
	//				 HashSet<IntrinsicElectronicData> VisitedSteps)
	//{
	//	Upstream = PreviousStep;
	//	VisitedSteps.Add(InData);
	//	if (InData.Present.Data.connections.Count == 0)
	//	{
	//		InData.Present.FindPossibleConnections();
	//	}

	//	foreach (ElectricalOIinheritance ToNode in InData.Present.Data.connections)
	//	{
	//		//Logger.Log(" InData.Present.Data.connections > " + ToNode.InData.Categorytype);
	//		if (!VisitedSteps.Contains(ToNode.InData))
	//		{


	//			CableLine Line = null;
	//			var Newnode = ElectricalSynchronisation.GetStep();
	//			MasterClass.LiveSteps.Add(Newnode);

	//			if (CompatibleCables.Contains(ToNode.InData.Categorytype))
	//			{
	//				Line = new CableLine();
	//				Line.TheStart = ToNode.InData;
	//				var Wire = ToNode as WireConnect;

	//				Wire.LineExplore(Line, VisitedSteps);

	//				if (Line.TheEnd != null)
	//				{
	//					Newnode.InData = Line.TheEnd;
	//					Newnode.Upstream = this;
	//					if (RegisterJump(MasterClass, this, ToNode, Newnode))
	//					{
	//						Newnode.Jump(MasterClass, this, QuickAdd(VisitedSteps, Line));
	//						continue;
	//					}
	//					else {
	//						continue;
	//					}
	//				}

	//			}

	//			Newnode.InData = ToNode.InData;
	//			Newnode.Upstream = this;

	//			if (RegisterJump(MasterClass, this, ToNode, Newnode))
	//			{
	//				Newnode.Jump(MasterClass, this, new HashSet<IntrinsicElectronicData>(VisitedSteps));
	//			}
	//		}
	//	}
	//}


	//public bool RegisterJump(
	//	ElectricalDirections MasterClass,
	//	ElectricalDirectionStep PreviousStep,
	//	ElectricalOIinheritance ToNode,
	//	ElectricalDirectionStep ToStep
	//)
	//{
	//	//Logger.Log("a");
	//	if (PreviousStep != null)
	//	{
	//		//Logger.Log("B" + PreviousStep.Node.InData.Categorytype);
	//		foreach (var ff in ToNode.InData.ConnectionReaction)
	//		{
	//			//Logger.Log("FF" + ff);
	//		}
	//		if (ToNode.InData.ConnectionReaction.ContainsKey(PreviousStep.InData.Categorytype))
	//		{
	//			//Logger.Log("c");
	//			if (ToNode.InData.ConnectionReaction[PreviousStep.InData.Categorytype].DirectionReaction
	//				|| ToNode.InData.ConnectionReaction[PreviousStep.InData.Categorytype].ResistanceReaction)
	//			{
	//				ElectricalOIinheritance SourceInstancPowerSupply = MasterClass.RelatedNode.Node;
	//				//Logger.Log("d");
	//				if (!ToNode.Data.ResistanceToConnectedDevices.ContainsKey(SourceInstancPowerSupply))
	//				{
	//					ToNode.Data.ResistanceToConnectedDevices[SourceInstancPowerSupply] = new
	//						Dictionary<ElectricalOIinheritance, ElectronicStepAndProcessed>(); //ElectricalDirectionStep
	//				}
	//				if (!ToNode.Data.ResistanceToConnectedDevices[SourceInstancPowerSupply].ContainsKey(PreviousStep.InData.Present))
	//				{
	//					ToNode.Data.ResistanceToConnectedDevices[SourceInstancPowerSupply][PreviousStep.InData.Present] = new ElectronicStepAndProcessed();
	//				}
	//				ToNode.Data.ResistanceToConnectedDevices[SourceInstancPowerSupply][PreviousStep.InData.Present].Steps.Add(ToStep);
	//				ToNode.Data.ResistanceToConnectedDevices[SourceInstancPowerSupply][PreviousStep.InData.Present].BeenProcessed = false;
	//				//Logger.Log("HHHH");
	//				//ResistanceToConnectedDevices is It stores whats connected to what so am Connected to this
	//				MasterClass.RelatedNode.Node.connectedDevices.Add(ToNode);
	//				ElectricalSynchronisation.InitialiseResistanceChange.Add(ToNode.InData.ControllingDevice);

	//				return (!ToNode.InData.ConnectionReaction[PreviousStep.InData.Categorytype].DirectionReactionA.YouShallNotPass);
	//			}
	//		}
	//	}
	//	return (true);
	//}

	//public HashSet<IntrinsicElectronicData> QuickAdd(HashSet<IntrinsicElectronicData> VisitedSteps, CableLine line)
	//{
	//	var hashset = new HashSet<IntrinsicElectronicData>(VisitedSteps);

	//	if (line != null)
	//	{
	//		hashset.UnionWith(line.Covering);
	//	}
	//	return (hashset);
	//}


}