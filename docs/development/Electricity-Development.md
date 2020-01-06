# Electricity Development

#### The Idea:

To simulate a simple version of real world circuits. We are assuming that 1 wire actually equals a 2 core wire and everything attached to the circuit is always wired in parallel. Also we need to avoid keeping lists of equipment, just like in the real world, the accumulative resistance of a circuit should apply its load on any power source at the point of connection. 

#### Direction of flow:
 
The StructuredWired prefab and the IOElectricity interface accounts for a direction start and end indicator but electricity flows in both directions on unitystation(because we assume everything is wired in parallel). This will simplify the circuits.

### The IOElectricity Interface:

The IOElectricity Interface can be added to any object that needs to be wired into a circuit. The wires (in [WireConnect.cs](https://github.com/unitystation/unitystation/blob/electricity/dev/UnityProject/Assets/Scripts/Electricity/Wire/WireConnect.cs)) search for all IOElectricity interfaces in the matrix on neighboring tiles and also on their tile position. The two connection points are then extracted and compared against the connection points of the wire doing the searching and if two connection points match up then the adjacent tile is added to a connected list on that wire, thus completing a connection.

The method that does the searching can be seen [here](https://github.com/unitystation/unitystation/blob/adfab36e750121377570d8b110c384d7472e827d/UnityProject/Assets/Scripts/Electricity/Wire/WireConnect.cs#L38).

And you can see how a connection is validated in the [ConnectionMap.cs](https://github.com/unitystation/unitystation/blob/electricity/dev/UnityProject/Assets/Scripts/Electricity/ConnectionMap.cs) helper.

To pass electricity call the ElectricityInput method with the current tick number (tick rate has not been set up yet and flow is just manually triggered by a context menu. More on that soon)

#### IOElectricity.cs:
```cs
using Electricity;
using UnityEngine;
/// <summary>
/// For any object that can conduct electricity
/// Handles the input/output path
/// </summary>
public interface IElectricityIO
{
	/// <summary>
	/// The input path to the object/wire
	/// </summary>
	void ElectricityInput(int currentTick);

	/// <summary>
	/// The output path of the object/wire that is passing electricity through it
	/// </summary>
	void ElectricityOutput(int currentTick);

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
```
### To see it in action:

 - Checkout the electricity/dev branch
 - Open the electricty_dev scene n Assets/Scenes/
 - Press play and spawn as any role
 - In the Scene Window find the test area and zoom in to focus on the circuit:

![](https://i.imgur.com/3zovOjo.png)

 - Find a StructuredWire in the hierarchy (anyone will do as electricity will flow in both directions):

![](https://i.imgur.com/oBdnFhw.png)

 - With one of the StructuredWire's selected go to the Inspector and find the WireConnect component and right click it then select GenerateTestCurrent option

![](https://i.imgur.com/DKp1lT6.png)

 - Now you should see all of the Yellow Sphere Gizmos show up which means that electricity has passed through that wire successfully:

 ![](https://i.imgur.com/1Di2EWJ.png)


## TODOS:
 
 - Connect the test machines to the circuit (SMES, APC and Field Gen) using the IOElectricity Interface
 - Calculate the total resistance of the circuit along with voltage and current
 - Generate the electricity from the SMES
 - Monitor the properties of the circuit




