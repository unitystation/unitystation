
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

		return (AdjacentConnections.pointA == validDirection || AdjacentConnections.pointB == validDirection);
	}

	public static bool IsConnectedToTileOverlap(Connection ConnectionDirection, ConnPoint AdjacentConnections)
	{
		switch (ConnectionDirection)
		{   // Intentional Fallthrough for these cases
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
				{   // All of the cases above run this code
					return (AdjacentConnections.pointA == ConnectionDirection || AdjacentConnections.pointB == ConnectionDirection);
				}
			default:
				{   // Unspecified behavior in all other cases
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
	/// Get connections pointing towards tile (ex. if direction equals north, get all south connections from tile at north)
	/// </summary>
	/// <param name="direction">direction to target tile</param>
	/// <param name="connections">possible connections</param>
	/// <returns>true if found any connections</returns>
	public static bool GetConnectionsTargeting(Connection direction, out HashSet<Connection> connections)
	{
		switch (direction)
		{
			case Connection.North:
				connections = new HashSet<Connection>() {
					Connection.South,
					Connection.SouthEast,
					Connection.SouthWest
				};
				return true;
			case Connection.NorthEast:
				connections = new HashSet<Connection>() {
					Connection.SouthWest
				};
				return true;
			case Connection.East:
				connections = new HashSet<Connection>() {
					Connection.West,
					Connection.NorthWest,
					Connection.SouthWest
				};
				return true;
			case Connection.SouthEast:
				connections = new HashSet<Connection>() {
					Connection.NorthWest
				};
				return true;
			case Connection.South:
				connections = new HashSet<Connection>() {
					Connection.North,
					Connection.NorthEast,
					Connection.NorthWest
				};
				return true;
			case Connection.SouthWest:
				connections = new HashSet<Connection>() {
					Connection.NorthEast
				};
				return true;
			case Connection.West:
				connections = new HashSet<Connection>() {
					Connection.East,
					Connection.NorthEast,
					Connection.SouthEast
				};
				return true;
			case Connection.NorthWest:
				connections = new HashSet<Connection>() {
					Connection.SouthEast
				};
				return true;
		}

		connections = null;
		return false;
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
	AnyNeighbour
}

public struct ConnPoint
{
	public Connection pointA;
	public Connection pointB;
}
