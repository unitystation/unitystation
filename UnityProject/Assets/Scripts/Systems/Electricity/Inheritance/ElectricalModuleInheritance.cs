using System.Collections.Generic;
using UnityEngine;
using Systems.Electricity.NodeModules;

namespace Systems.Electricity.Inheritance
{
	public class ElectricalModuleInheritance : MonoBehaviour, IServerDespawn
	{
		[HideInInspector] public ElectricalModuleTypeCategory ModuleType;
		public ElectricalNodeControl ControllingNode;
		[HideInInspector] public HashSet<ElectricalUpdateTypeCategory> RequiresUpdateOn;

		public virtual void OnStartServer() {
		}

		public virtual void TurnOffCleanup(){
		}

		public virtual void TurnOnSupply() {
		}

		public virtual void TurnOffSupply() {
		}

		public virtual void OnDespawnServer(DespawnInfo info){ }

		public virtual void PowerUpdateStructureChange(){
		}

		public virtual void PowerUpdateStructureChangeReact(){
		}

		public virtual void InitialPowerUpdateResistance(){
		}

		public virtual void PowerUpdateResistanceChange() {
		}

		public virtual void PowerNetworkUpdate(){
		}

		public virtual void PowerUpdateCurrentChange(){
		}

		public virtual void PotentialDestroyed(){
		}

		public virtual void DirectionInput(ElectricalOIinheritance SourceInstance, ElectricalOIinheritance ComingFrom, ElectricalNodeControl ComplexPassOn){
		}

		public virtual VIRCurrent  ModifyElectricityInput(VIRCurrent Current,
			ElectricalOIinheritance SourceInstance,
			IntrinsicElectronicData ComingFromm){
			return (Current);
		}

		public virtual VIRCurrent  ModifyElectricityOutput(VIRCurrent  Current, ElectricalOIinheritance SourceInstance){
			return (Current);
		}

		public virtual ResistanceWrap ModifyResistanceInput(ResistanceWrap Resistance, ElectricalOIinheritance SourceInstance, IntrinsicElectronicData ComingFrom){
			return (Resistance);
		}
		public virtual ResistanceWrap ModifyResistancyOutput(ResistanceWrap Resistance, ElectricalOIinheritance SourceInstance){
			return (Resistance);
		}
	}
}
