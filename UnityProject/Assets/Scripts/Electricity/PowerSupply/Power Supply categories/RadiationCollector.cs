using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class RadiationCollector : InputTrigger, IElectricalNeedUpdate {
	public PowerSupply powerSupply;
	[SyncVar(hook="UpdateState")]
	public bool isOn = false;

	public bool FirstStart = true;

	public bool ChangeToOff = false;
	public int DirectionStart = 0;
	public int DirectionEnd = 9;


	public float current = 20;
	public float Previouscurrent = 20;

	public PowerTypeCategory ApplianceType = PowerTypeCategory.RadiationCollector;
	public HashSet<PowerTypeCategory> CanConnectTo = new HashSet<PowerTypeCategory>(){
		PowerTypeCategory.StandardCable,
		PowerTypeCategory.HighVoltageCable,
	};

	public void PowerUpdateStructureChange(){
		//Logger.Log ("PowerUpdateStructureChange");
		powerSupply.PowerUpdateStructureChange ();
	}
	public void PowerUpdateStructureChangeReact(){
		powerSupply.PowerUpdateStructureChangeReact ();
	}
	public void PowerUpdateResistanceChange(){
		Logger.Log ("PowerUpdateResistanceChange");
		powerSupply.PowerUpdateResistanceChange ();
	}
	public void PowerUpdateCurrentChange (){
		//Logger.Log ("PowerUpdateCurrentChange");
		powerSupply.PowerUpdateCurrentChange ();
	}

	public void PowerNetworkUpdate (){
		powerSupply.PowerNetworkUpdate ();
		if (current != Previouscurrent) {
			powerSupply.SupplyingCurrent = current;
			Previouscurrent = current;
			ElectricalSynchronisation.CurrentChange = true;
		}
		if (ChangeToOff) {
			ChangeToOff = false;
			Logger.Log ("Turning off");
			ElectricalSynchronisation.RemoveSupply (this,ApplianceType);
			ElectricalSynchronisation.CurrentChange = true;
			powerSupply.TurnOffSupply(); 
		}
	}
	public override void OnStartClient(){
		base.OnStartClient();
		powerSupply.CanConnectTo = CanConnectTo;
		powerSupply.Categorytype = ApplianceType;
		powerSupply.DirectionStart = DirectionStart;
		powerSupply.DirectionEnd = DirectionEnd;
		powerSupply.SupplyingCurrent = 20;
		Logger.Log ("Starting");
		//UpdateState(isOn);
	}

	void UpdateState(bool _isOn){
		isOn = _isOn;

		if(isOn){
			ElectricalSynchronisation.AddSupply (this,ApplianceType);
			ElectricalSynchronisation.StructureChangeReact = true;
			ElectricalSynchronisation.ResistanceChange = true;
			ElectricalSynchronisation.CurrentChange = true;
			Logger.Log ("on");
			powerSupply.TurnOnSupply(); 


		} else {
			Logger.Log ("off");
			ChangeToOff = true;
		}
	}
	

	public override void Interact(GameObject originator, Vector3 position, string hand)
	{
		//Logger.Log ("yo Interact");
		//Interact stuff with the SMES here
		if (!isServer) {
			InteractMessage.Send(gameObject, hand);
		} else {
			isOn = !isOn;
		}
	}

	public void OnDestroy(){
		ElectricalSynchronisation.StructureChangeReact = true;
		ElectricalSynchronisation.ResistanceChange = true;
		ElectricalSynchronisation.CurrentChange = true;
		//Then you can destroy
	}
}
