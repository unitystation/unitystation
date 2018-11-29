using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;
using UnityEngine.Events;

public class InLineDevice : NetworkBehaviour, IElectricityIO, IProvidePower 
{

	public int DirectionStart;
	public int DirectionEnd;

	public ElectronicData Data {get; set;} = new ElectronicData();
	public IntrinsicElectronicData InData  {get; set;} = new IntrinsicElectronicData();

	public HashSet<IElectricityIO> DirectionWorkOnNextList {get; set;} = new HashSet<IElectricityIO> ();
	public HashSet<IElectricityIO> DirectionWorkOnNextListWait {get; set;} = new HashSet<IElectricityIO> ();
	public HashSet<IElectricityIO> ResistanceWorkOnNextList {get; set;} = new HashSet<IElectricityIO> ();
	public HashSet<IElectricityIO> ResistanceWorkOnNextListWait {get; set;} = new HashSet<IElectricityIO> ();
	public HashSet<IElectricityIO> connectedDevices {get; set;} = new HashSet<IElectricityIO>();

	public RegisterObject registerTile;
	private Matrix matrix => registerTile.Matrix;
	public bool connected = false;

	public IInLineDevices RelatedDevice;

	public void FindPossibleConnections(){
		Data.connections.Clear();
		Data.connections = ElectricityFunctions.FindPossibleConnections(
			transform.localPosition,
			matrix,
			InData.CanConnectTo,
			GetConnPoints()
		);
		if (Data.connections.Count > 0){
			connected =  true;
		}

	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		//Not working for some reason:
		//registerTile = gameObject.GetComponent<RegisterItem>();
		StartCoroutine(WaitForLoad());
	}

	IEnumerator WaitForLoad()
	{
		yield return new WaitForSeconds(2f);
		FindPossibleConnections();
	}

	public void TurnOnSupply(float current){
		Data.SupplyingCurrent = current;
	}

	public void TurnOffSupply(){
		Electricity supply = new Electricity();
		RemoveSupply (this.GameObject());
	}

	public void PowerUpdateStructureChange(){
		FlushConnectionAndUp ();
	}

	public void PowerUpdateStructureChangeReact(){
		ElectricityFunctions.CircuitSearchLoop(this, this);
	}
	public void PowerUpdateResistanceChange(){
		//CircuitSearchLoop ();
		FlushResistanceAndUp (this.gameObject);
		if (connectedDevices.Count > 0) {
			foreach (IElectricityIO ConnectedDevice in connectedDevices) {
				ConnectedDevice.ResistanceInput (ElectricalSynchronisation.currentTick, 1.11111111f, this.gameObject, null);
			}
			ElectricityFunctions.CircuitResistanceLoop (this, this);
		}
	}
	public void PowerUpdateCurrentChange (){
		FlushSupplyAndUp (this.gameObject);

		if (connectedDevices.Count > 0) {
			int InstanceID = this.gameObject.GetInstanceID ();
			float Resistance = ElectricityFunctions.WorkOutResistance (Data.ResistanceComingFrom [InstanceID]);
			float Voltage = Data.SupplyingCurrent * Resistance;
			ElectricityOutput (ElectricalSynchronisation.currentTick, Data.SupplyingCurrent, this.gameObject);
		}
	}

	public void PowerNetworkUpdate (){
	}
				
	public void DirectionInput(int tick, GameObject SourceInstance, IElectricityIO ComingFrom, IElectricityIO PassOn  = null){
		ElectricityFunctions.DirectionInput (tick, SourceInstance,ComingFrom, this);
	} 
	public void DirectionOutput(int tick, GameObject SourceInstance) {
		ElectricityFunctions.DirectionOutput (tick, SourceInstance, this);
		int SourceInstanceID = SourceInstance.GetInstanceID();
		Data.DownstreamCount = Data.Downstream [SourceInstanceID].Count;
		Data.UpstreamCount = Data.Upstream [SourceInstanceID].Count;
		//Logger.Log (this.gameObject.GetInstanceID().ToString() + " <ID | Downstream = "+Downstream[SourceInstanceID].Count.ToString() + " Upstream = " + Upstream[SourceInstanceID].Count.ToString (), Category.Electrical);
	}
		
	public void ResistanceInput(int tick, float Resistance, GameObject SourceInstance, IElectricityIO ComingFrom  ){
		Resistance = RelatedDevice.ModifyResistanceInput (tick, Resistance, SourceInstance, ComingFrom);
		ElectricityFunctions.ResistanceInput (tick, Resistance, SourceInstance, ComingFrom, this);
	}

	public void ResistancyOutput(int tick, GameObject SourceInstance){
		int SourceInstanceID = SourceInstance.GetInstanceID ();
		float Resistance = ElectricityFunctions.WorkOutResistance (Data.ResistanceComingFrom [SourceInstanceID]);
		Resistance = RelatedDevice.ModifyResistancyOutput (tick, Resistance, SourceInstance);
		ElectricityFunctions.ResistancyOutput(tick, Resistance, SourceInstance, this);
	}
	public void ElectricityInput(int tick, float Current, GameObject SourceInstance,  IElectricityIO ComingFrom){ 
		Current = RelatedDevice.ModifyElectricityInput (tick, Current, SourceInstance, ComingFrom);
		//Logger.Log(Current.ToString() + "yoree");
		ElectricityFunctions.ElectricityInput (tick, Current, SourceInstance, ComingFrom, this);
	}
		
	public void ElectricityOutput(int tick, float Current, GameObject SourceInstance){
		Current = RelatedDevice.ModifyElectricityOutput (tick, Current, SourceInstance);
		//Logger.Log (CurrentInWire.ToString () + " How much current", Category.Electrical);
		if (Current != 0) {
		ElectricityFunctions.ElectricityOutput(tick,Current,SourceInstance,this);
		}
		Data.ActualCurrentChargeInWire = ElectricityFunctions.WorkOutActualNumbers(this);
		Data.CurrentInWire = Data.ActualCurrentChargeInWire.Current;
		Data.ActualVoltage = Data.ActualCurrentChargeInWire.Voltage;
		Data.EstimatedResistance = Data.ActualCurrentChargeInWire.EstimatedResistant;

	}

	public void SetConnPoints(int DirectionEndin, int DirectionStartin){
	}

	public GameObject GameObject(){
		return gameObject;
	}

	public ConnPoint GetConnPoints(){
		ConnPoint points = new ConnPoint();
		points.pointA = DirectionStart;
		points.pointB = DirectionEnd;
		return points;
	}

	public void FlushConnectionAndUp ( ){
		ElectricityFunctions.PowerSupplies.FlushConnectionAndUp (this);
		InData.ControllingDevice.PotentialDestroyed ();
	}
	public void FlushResistanceAndUp (  GameObject SourceInstance = null  ){
		ElectricityFunctions.PowerSupplies.FlushResistanceAndUp (this, SourceInstance);
	}
	public void FlushSupplyAndUp ( GameObject SourceInstance = null ){
		ElectricityFunctions.PowerSupplies.FlushSupplyAndUp (this, SourceInstance);
	}
	public void RemoveSupply( GameObject SourceInstance = null){
		ElectricityFunctions.PowerSupplies.RemoveSupply (this, SourceInstance);
	}
		
	[ContextMethod("Details","Magnifying_glass")]
	public void ShowDetails(){
		Logger.Log("connections " + (Data.connections.Count.ToString()), Category.Electrical);
		Logger.Log ("ID " + (this.GetInstanceID ()), Category.Electrical);
		Logger.Log ("Type " + (InData.Categorytype.ToString()), Category.Electrical);
		Logger.Log ("Can connect to " + (string.Join(",", InData.CanConnectTo)), Category.Electrical);
		Logger.Log("UpstreamCount " + (Data.UpstreamCount.ToString()), Category.Electrical);
		Logger.Log("DownstreamCount " + (Data.DownstreamCount.ToString()), Category.Electrical);
		Logger.Log("CurrentInWire " + (Data.CurrentInWire.ToString()), Category.Electrical);
		Logger.Log("ActualVoltage " + (Data.ActualVoltage.ToString()), Category.Electrical);
		Logger.Log("EstimatedResistance " + (Data.EstimatedResistance.ToString()), Category.Electrical);
	}
}
