using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectricalModuleInheritance : MonoBehaviour, IServerDespawn
{
	public ElectricalModuleTypeCategory ModuleType;
	public ElectricalNodeControl ControllingNode;
	public HashSet<ElectricalUpdateTypeCategory> RequiresUpdateOn;

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

	public virtual void DirectionInput(GameObject SourceInstance, ElectricalOIinheritance ComingFrom, ElectricalNodeControl ComplexPassOn){
	}

	public virtual float ModifyElectricityInput(float Current, GameObject SourceInstance, ElectricalOIinheritance ComingFrom){
		return (Current);
	}

	public virtual float ModifyElectricityOutput(float Current, GameObject SourceInstance){
		return (Current);
	}

	public virtual float ModifyResistanceInput(float Resistance, GameObject SourceInstance, ElectricalOIinheritance ComingFrom){
		return (Resistance);
	}
	public virtual float ModifyResistancyOutput(float Resistance, GameObject SourceInstance){
		return (Resistance);
	}
}
