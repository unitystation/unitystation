using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// For any object that can conduct electricity
/// Handles the input/output path
/// </summary>
public interface IElectricalOIinheritance
{
	ElectronicData Data {get; set;}
	IntrinsicElectronicData InData  {get; set;}
	HashSet<ElectricalOIinheritance> connectedDevices {get; set;}

	void FindPossibleConnections ();
	/// <summary>
	/// The input path to the object/wire
	/// 
	/// currentTick = the current tick rate count
	/// </summary>
	void ElectricityInput(float Current, GameObject SourceInstance,  ElectricalOIinheritance ComingFrom);

	/// <summary>
	/// The output path of the object/wire that is passing electricity through it
	/// </summary>
	void ElectricityOutput(float Current, GameObject SourceInstance);

	/// <summary>
	/// Pass resistance with ID of the supplying machine
	/// </summary>
	void ResistanceInput(float Resistance, GameObject SourceInstance, ElectricalOIinheritance ComingFrom  );

	/// <summary>
	/// Passes it on to the next cable
	/// </summary>
	void ResistancyOutput(GameObject SourceInstance);

	/// <summary>
	///  Sets the upstream 
	/// </summary>
	void DirectionInput(GameObject SourceInstance, ElectricalOIinheritance ComingFrom, CableLine PassOn  = null);
	/// <summary>
	/// Sets the downstream and pokes the next one along 
	/// </summary>
	void DirectionOutput(GameObject SourceInstance);
	/// <summary>
	/// Flushs the connection and up. Flushes out everything
	/// </summary>
	void FlushConnectionAndUp ();


	/// <summary>
	/// Flushs the resistance and up. Cleans out resistance and current 
	/// </summary>
	void FlushResistanceAndUp ( GameObject SourceInstance = null );
	/// <summary>
	/// Flushs the supply and up. Cleans out the current 
	/// </summary>
	void FlushSupplyAndUp ( GameObject SourceInstance = null );

	void RemoveSupply (GameObject SourceInstance = null);

	/// <summary>
	///     Returns a struct with both connection points as members
	///     the connpoint connection positions are represented using 4 bits to indicate N S E W - 1 2 4 8
	///     Corners can also be used i.e.: 5 = NE (1 + 4) = 0101
	///     This is the edge of the location where the input connection enters the turf
	///     Use 0 for Machines or grills that can conduct electricity from being placed ontop of any wire configuration
	/// </summary>
	void SetConnPoints(int DirectionEnd, int DirectionStart);
	 
	ConnPoint GetConnPoints();

	//Return the GameObject that this interface is on
	GameObject GameObject();
}
