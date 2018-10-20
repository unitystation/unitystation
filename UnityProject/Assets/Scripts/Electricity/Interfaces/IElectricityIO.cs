using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// For any object that can conduct electricity
/// Handles the input/output path
/// </summary>
public interface IElectricityIO
{

	PowerTypeCategory Categorytype {get; set;}
	HashSet<PowerTypeCategory> CanConnectTo {get; set;}
	int FirstPresent {get; set;}
	Dictionary<int,HashSet<IElectricityIO>> Downstream {get; set;}
	Dictionary<int,HashSet<IElectricityIO>> Upstream {get; set;}
	Dictionary<int,Dictionary<IElectricityIO,float>> ResistanceTosource {get; set;}
	Dictionary<int,Dictionary<IElectricityIO,float>> CurrentComingFrom {get; set;}
	float ActualCurrentChargeInWire {get; set;}
	List<IElectricityIO> connections {get; set;}
	/// <summary>
	/// The input path to the object/wire
	/// 
	/// currentTick = the current tick rate count
	/// </summary>
	void ElectricityInput(int tick, float Current, GameObject SourceInstance,  IElectricityIO ComingFrom);

	/// <summary>
	/// The output path of the object/wire that is passing electricity through it
	/// </summary>
	void ElectricityOutput(int tick, float Current, GameObject SourceInstance);



	/// <summary>
	/// Pass resistance with ID of the supplying machine
	/// </summary>
	void ResistanceInput(int tick, float Resistance, GameObject SourceInstance, IElectricityIO ComingFrom  );

	/// <summary>
	/// Passes it on to the next cable
	/// </summary>
	void ResistancyOutput(int tick, float Resistance, GameObject SourceInstance);

	/// <summary>
	///  Sets the upstream 
	/// </summary>
	void DirectionInput(int tick, GameObject SourceInstance, IElectricityIO ComingFrom);
	/// <summary>
	/// Sets the downstream and pokes the next one along 
	/// </summary>
	void DirectionOutput(int tick, GameObject SourceInstance);


	/// <summary>
	///     Returns a struct with both connection points as members
	///     the connpoint connection positions are represented using 4 bits to indicate N S E W - 1 2 4 8
	///     Corners can also be used i.e.: 5 = NE (1 + 4) = 0101
	///     This is the edge of the location where the input connection enters the turf
	///     Use 0 for Machines or grills that can conduct electricity from being placed ontop of any wire configuration
	/// </summary>
	ConnPoint GetConnPoints();

	//Return the GameObject that this interface is on
	GameObject GameObject();
}
