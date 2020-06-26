
using System.Collections.Generic;
using UnityEngine;

public static class ConnectionMap
{
	/// <summary>
	/// Given the output position of a wire and an adjacent tile to check
	/// this method returns true if the adjacent tile has a connection 
	/// input point that is intersecting the origin tiles output point
	/// </summary>
	public static bool IsConnectedToTile(Connection ConnectionDirection, ConnPoint AdjacentConnections)
	{
		Connection validDirection;
		switch (ConnectionDirection)
		{
			case Connection.North:
			{
				validDirection = Connection.South;
				break;
			}
			case Connection.NorthEast:
			{
				validDirection = Connection.SouthWest;
				break;
			}
			case Connection.East:
			{
				validDirection = Connection.West;
				break;
			}
			case Connection.SouthEast:
			{
				validDirection = Connection.NorthWest;
				break;
			}
			case Connection.South:
			{
				validDirection = Connection.North;
				break;
			}
			case Connection.SouthWest:
			{
				validDirection = Connection.NorthEast;
				break;
			}
			case Connection.West:
			{
				validDirection = Connection.East;
				break;
			}
			case Connection.NorthWest:
			{
				validDirection = Connection.SouthEast;
				break;
			}
			case Connection.Overlap:
			{
				validDirection = Connection.Overlap;
				break;
			}
			case Connection.MachineConnect:
			{
				validDirection = Connection.MachineConnect;
				break;
			}
			default:
			{
				return false;
			}
		}

		// check if our connection is pointing towards tile with "SurroundingTiles" connection
		// Overlap will never point towards other tile
		if (validDirection != Connection.Overlap)
		{
			return	(AdjacentConnections.pointA == validDirection || AdjacentConnections.pointB == validDirection)
				||	(AdjacentConnections.pointA == Connection.SurroundingTiles || AdjacentConnections.pointB == Connection.SurroundingTiles);
		}

		return (AdjacentConnections.pointA == validDirection || AdjacentConnections.pointB == validDirection);
	}

	public static bool IsConnectedToTileOverlap(Connection ConnectionDirection, ConnPoint AdjacentConnections)
	{
		switch (ConnectionDirection)
		{	// Intentional Fallthrough for these cases
			case Connection.North:
			case Connection.NorthEast:
			case Connection.East:
			case Connection.SouthEast:
			case Connection.South:
			case Connection.SouthWest:
			case Connection.West:
			case Connection.NorthWest:
			case Connection.Overlap:
			case Connection.MachineConnect:
			{	// All of the cases above run this code
				return (AdjacentConnections.pointA == ConnectionDirection || AdjacentConnections.pointB == ConnectionDirection);
			}
			default:
			{	// Unspecified behavior in all other cases
				return false;
			}
		}
	}

	/// <summary>
	/// Convert Connection to Vector3Int
	/// </summary>
	/// <returns>connection as Vector3Int</returns>
	public static Vector3Int GetDirectionFromConnection(Connection connection)
	{
		switch (connection)
		{
			case Connection.North:
				return new Vector3Int(0, 1, 0);
			case Connection.NorthEast:
				return new Vector3Int(1, 1, 0);
			case Connection.East:
				return new Vector3Int(1, 0, 0);
			case Connection.SouthEast:
				return new Vector3Int(1, -1, 0);
			case Connection.South:
				return new Vector3Int(0, -1, 0);
			case Connection.SouthWest:
				return new Vector3Int(-1, -1, 0);
			case Connection.West:
				return new Vector3Int(-1, 0, 0);
			case Connection.NorthWest:
				return new Vector3Int(-1, 1, 0);
			case Connection.Overlap:
			default:
				return Vector3Int.zero;
		}
	}

	/// <summary>
	/// Get connections pointing towards tile [ex. if direction equals north, get all south connections(SW, S, SE) from tile at north]
	/// </summary>
	/// <param name="direction">direction to target tile</param>
	/// <param name="connections">possible connections</param>
	/// <returns>HashSet of possible connections</returns>
	public static HashSet<Connection> GetConnectionsTargeting(Connection direction)
	{
		HashSet<Connection> result = null;
		switch (direction)
		{
			case Connection.North:
				result = new HashSet<Connection>() {
					Connection.South,
					Connection.SouthEast,
					Connection.SouthWest
				};
				break;
			case Connection.NorthEast:
				result =  new HashSet<Connection>() {
					Connection.SouthWest
				};
				break;
			case Connection.East:
				result =  new HashSet<Connection>() {
					Connection.West,
					Connection.NorthWest,
					Connection.SouthWest
				};
				break;
			case Connection.SouthEast:
				result =  new HashSet<Connection>() {
					Connection.NorthWest
				};
				break;
			case Connection.South:
				result =  new HashSet<Connection>() {
					Connection.North,
					Connection.NorthEast,
					Connection.NorthWest
				};
				break;
			case Connection.SouthWest:
				result =  new HashSet<Connection>() {
					Connection.NorthEast
				};
				break;
			case Connection.West:
				result =  new HashSet<Connection>() {
					Connection.East,
					Connection.NorthEast,
					Connection.SouthEast
				};
				break;
			case Connection.NorthWest:
				result =  new HashSet<Connection>() {
					Connection.SouthEast
				};
				break;
			case Connection.Overlap:
				result = new HashSet<Connection>()
				{
					Connection.Overlap
				};
				break;
		}

		// if result != null add new connection
		result?.Add(Connection.SurroundingTiles);

		return result;
	}

}

public enum Connection
{
	NA,
	North,
	NorthEast,
	East,
	SouthEast,
	South,
	SouthWest,
	West,
	NorthWest,
	Overlap,
	MachineConnect,
	SurroundingTiles
}

public struct ConnPoint
{
	public Connection pointA;
	public Connection pointB;
}
