using UnityEngine;
using Electricity;

/// <summary>
/// For any object that can conduct electricity
/// Handles the input/output path
/// </summary>
public interface IElectricityIO
{
	/// <summary>
	/// The input path to the object/wire
	/// 
	/// currentTick = the current tick rate count
	/// </summary>
	void ElectricityInput(int currentTick, Electricity.Electricity electricity);

	/// <summary>
	/// The output path of the object/wire that is passing electricity through it
	/// </summary>
	void ElectricityOutput(int currentTick, Electricity.Electricity electricity);

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
