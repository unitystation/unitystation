/// <summary>
/// For any object that can conduct electricity
/// Handles the input/output path
/// </summary>
public interface IElectricityIO
{
	/// <summary>
	/// The input path to the object/wire
	/// </summary>
	void ElectricityInput();

	/// <summary>
	/// The output path of the object/wire that is passing electricity through it
	/// </summary>
	void ElectricityOutput();

	/// <summary>
	///     Return the starting dir of this input in a turf, using 4 bits to indicate N S E W - 1 2 4 8
	///     Corners can also be used i.e.: 5 = NE (1 + 4) = 0101
	///     This is the edge of the location where the input connection enters the turf
	///     Use 0 for Machines or grills that can conduct electricity from being placed ontop of any wire configuration
	/// </summary>
	int InputPosition();

	/// <summary>
	///     The output position of the connection in a turf, using 4 bits to indicate N S E W - 1 2 4 8
	///     Corners can also be used i.e.: 5 = NE (1 + 4) = 0101
	///     This is the edge of the location where the input connection exits the turf
	///     Can be 0 for objects that do not output electricity
	/// </summary>
	int OutputPosition();
}

