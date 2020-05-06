
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
}

public struct ConnPoint
{
	public Connection pointA;
	public Connection pointB;
}
