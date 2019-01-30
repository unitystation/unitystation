using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CableLine : IElectricityIO {
	public GameObject InitialGenerator;
	public IElectricityIO TheStart;
	public IElectricityIO TheEnd;
	public List<IElectricityIO> Covering = new List<IElectricityIO>();


	public void DirectionInput(int tick, GameObject SourceInstance, IElectricityIO ComingFrom, CableLine PassOn  = null){
		JumpToOtherEnd (SourceInstance, ComingFrom);
	}

	public void JumpToOtherEnd(GameObject SourceInstance,IElectricityIO ComingFrom){


		if (ComingFrom == TheStart) {

			TheEnd.DirectionInput(0, SourceInstance, ComingFrom);
		} else if (ComingFrom == TheEnd) {
			TheStart.DirectionInput(0, SourceInstance, ComingFrom);
		}
	}
	public void DirectionOutput(int tick, GameObject SourceInstance){
		
	}
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}


	public int DirectionStart;
	public int DirectionEnd;
	public ElectronicData Data { get; set; } = new ElectronicData();
	public IntrinsicElectronicData InData { get; set; } = new IntrinsicElectronicData();
	public HashSet<IElectricityIO> connectedDevices { get; set; } = new HashSet<IElectricityIO>();


	IEnumerator WaitForLoad()
	{
		yield return new WaitForSeconds(1f);
		FindPossibleConnections();
	}

	public virtual void FindPossibleConnections()
	{

	}

	public ConnPoint GetConnPoints()
	{
		ConnPoint conns = new ConnPoint();
		conns.pointA = DirectionStart;
		conns.pointB = DirectionEnd;
		return conns;
	}

	public int InputPosition()
	{
		return DirectionStart;
	}

	public int OutputPosition()
	{
		return DirectionEnd;
	}

	public GameObject GameObject()
	{
		return null;
	}



	public virtual void ResistanceInput(int tick, float Resistance, GameObject SourceInstance, IElectricityIO ComingFrom)
	{
	}

	public virtual void ResistancyOutput(int tick, GameObject SourceInstance)
	{
	}
	public virtual void ElectricityInput(int tick, float Current, GameObject SourceInstance, IElectricityIO ComingFrom)
	{
	}

	public virtual void ElectricityOutput(int tick, float Current, GameObject SourceInstance)
	{
	}
	public virtual void SetConnPoints(int DirectionEndin, int DirectionStartin)
	{
		DirectionEnd = DirectionEndin;
		DirectionStart = DirectionStartin;
	}
		
	public virtual void FlushConnectionAndUp()
	{
		ElectricalDataCleanup.PowerSupplies.FlushConnectionAndUp(this);
		InData.ControllingDevice.PotentialDestroyed();
	}
	public virtual void FlushResistanceAndUp(GameObject SourceInstance = null)
	{
		ElectricalDataCleanup.PowerSupplies.FlushResistanceAndUp(this, SourceInstance);
	}
	public virtual void FlushSupplyAndUp(GameObject SourceInstance = null)
	{
		ElectricalDataCleanup.PowerSupplies.FlushSupplyAndUp(this, SourceInstance);
	}
	public virtual void RemoveSupply(GameObject SourceInstance = null)
	{
		ElectricalDataCleanup.PowerSupplies.RemoveSupply(this, SourceInstance);
	}


	public void PassOnFlushSupplyAndUp(IElectricityIO ComingFrom,GameObject SourceInstance = null)
	{
		if (ComingFrom == TheStart) {

			TheEnd.FlushSupplyAndUp(SourceInstance);
		} else if (ComingFrom == TheEnd) {
			TheStart.FlushSupplyAndUp(SourceInstance);
		}
	}

	public virtual void PassOnRemoveSupply(IElectricityIO ComingFrom,GameObject SourceInstance = null)
	{
		if (ComingFrom == TheStart) {

			TheEnd.RemoveSupply(SourceInstance);
		} else if (ComingFrom == TheEnd) {
			TheStart.RemoveSupply(SourceInstance);
		}
	}

}
