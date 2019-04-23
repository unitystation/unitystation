
public static class ConnectionMap
{
	/// <summary>
	/// Given the output position of a wire and an adjacent tile to check
	/// this method returns true if the adjacent tile has a connection 
	/// input point that is intersecting the origin tiles output point
	/// 
	/// originP = the two connection points of the WireTile that is doing the checking
	/// adjP = the two connection points of the WireTile that is being checked
	/// Adjtile = the direction to the adjacent tile
	/// </summary>
	public static bool IsConnectedToTile(Connection ConnectionDirection, ConnPoint AdjacentConnections)
	{
		switch (ConnectionDirection)
		{
			case Connection.North:
				{
					if (AdjacentConnections.pointA == Connection.South || AdjacentConnections.pointB == Connection.South)
					{
						return (true);
					}
				}
				break;
			case Connection.NorthEast:
				{
					if (AdjacentConnections.pointA == Connection.SouthWest || AdjacentConnections.pointB == Connection.SouthWest)
					{
						return (true);
					}
				}
				break;
			case Connection.East:
				{
					if (AdjacentConnections.pointA == Connection.West || AdjacentConnections.pointB == Connection.West)
					{
						return (true);
					}
				}
				break;
			case Connection.SouthEast:
				{
					if (AdjacentConnections.pointA == Connection.NorthWest || AdjacentConnections.pointB == Connection.NorthWest)
					{
						return (true);
					}
				}
				break;
			case Connection.South:
				{
					if (AdjacentConnections.pointA == Connection.North || AdjacentConnections.pointB == Connection.North)
					{
						return (true);
					}
				}
				break;
			case Connection.SouthWest:
				{
					if (AdjacentConnections.pointA == Connection.NorthEast || AdjacentConnections.pointB == Connection.NorthEast)
					{
						return (true);
					}
				}
				break;
			case Connection.West:
				{
					if (AdjacentConnections.pointA == Connection.East || AdjacentConnections.pointB == Connection.East)
					{
						return (true);
					}
				}
				break;
			case Connection.NorthWest:
				{
					if (AdjacentConnections.pointA == Connection.SouthEast || AdjacentConnections.pointB == Connection.SouthEast)
					{
						return (true);
					}
				}
				break;
			case Connection.Overlap:
				{
					if (AdjacentConnections.pointA == Connection.Overlap || AdjacentConnections.pointB == Connection.Overlap)
					{
						return (true);
					}
				}
				break;
			case Connection.MachineConnect:
				{
					if (AdjacentConnections.pointA == Connection.MachineConnect || AdjacentConnections.pointB == Connection.MachineConnect)
					{
						return (true);
					}
				}
				break;
		}
		return (false);
	}


	public static bool IsConnectedToTileOverlap(Connection ConnectionDirection, ConnPoint AdjacentConnections)
	{
		switch (ConnectionDirection)
		{
			case Connection.North:
				{
					if (AdjacentConnections.pointA == Connection.North || AdjacentConnections.pointB == Connection.North)
					{
						return (true);
					}
				}
				break;
			case Connection.NorthEast:
				{
					if (AdjacentConnections.pointA == Connection.NorthEast || AdjacentConnections.pointB == Connection.NorthEast)
					{
						return (true);
					}
				}
				break;
			case Connection.East:
				{
					if (AdjacentConnections.pointA == Connection.East || AdjacentConnections.pointB == Connection.East)
					{
						return (true);
					}
				}
				break;
			case Connection.SouthEast:
				{
					if (AdjacentConnections.pointA == Connection.SouthEast || AdjacentConnections.pointB == Connection.SouthEast)
					{
						return (true);
					}
				}
				break;
			case Connection.South:
				{
					if (AdjacentConnections.pointA == Connection.South || AdjacentConnections.pointB == Connection.South)
					{
						return (true);
					}
				}
				break;
			case Connection.SouthWest:
				{
					if (AdjacentConnections.pointA == Connection.SouthWest || AdjacentConnections.pointB == Connection.SouthWest)
					{
						return (true);
					}
				}
				break;
			case Connection.West:
				{
					if (AdjacentConnections.pointA == Connection.West || AdjacentConnections.pointB == Connection.West)
					{
						return (true);
					}
				}
				break;
			case Connection.NorthWest:
				{
					if (AdjacentConnections.pointA == Connection.NorthWest || AdjacentConnections.pointB == Connection.NorthWest)
					{
						return (true);
					}
				}
				break;
			case Connection.Overlap:
				{
					if (AdjacentConnections.pointA == Connection.Overlap || AdjacentConnections.pointB == Connection.Overlap)
					{
						return (true);
					}
				}
				break;
			case Connection.MachineConnect:
				{
					if (AdjacentConnections.pointA == Connection.MachineConnect || AdjacentConnections.pointB == Connection.MachineConnect)
					{
						return (true);
					}
				}
				break;
		}
		return (false);
	}
}

	//Direction of the adjacent tile
	public enum Connection
{
	North = 0,
	NorthEast,
	East,
	SouthEast,
	South, 
	SouthWest,
	West, 
	NorthWest,
	Overlap,
	MachineConnect,
	NA
}

	public struct ConnPoint{
		public Connection pointA;
		public Connection pointB;
	}
