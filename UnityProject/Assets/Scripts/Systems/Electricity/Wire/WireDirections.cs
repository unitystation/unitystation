using System.Collections.Generic;

public static class WireDirections
{
	/// <summary>
	/// Maps two Connection directions to a sprite index to show the correct wire sprite
	/// </summary>
	private static Dictionary<(Connection, Connection), int> LogicToIndexMap = new Dictionary<(Connection, Connection), int>
	{
		{ (Connection.North, Connection.Overlap), 0 },
		{ (Connection.South, Connection.Overlap), 1 },
		{ (Connection.East, Connection.Overlap), 2 },
		{ (Connection.NorthEast, Connection.Overlap), 3 },
		{ (Connection.SouthEast, Connection.Overlap), 4 },
		{ (Connection.West, Connection.Overlap), 5 },
		{ (Connection.NorthWest, Connection.Overlap), 6 },
		{ (Connection.SouthWest, Connection.Overlap), 7 },
		{ (Connection.North, Connection.South), 8 },
		{ (Connection.North, Connection.East), 9 },
		{ (Connection.North, Connection.NorthEast), 10 },
		{ (Connection.North, Connection.SouthEast), 11 },
		{ (Connection.North, Connection.West), 12 },
		{ (Connection.North, Connection.NorthWest), 13 },
		{ (Connection.North, Connection.SouthWest), 14 },
		{ (Connection.East, Connection.South), 15 },
		{ (Connection.NorthEast, Connection.South), 16 },
		{ (Connection.SouthEast, Connection.South), 17 },
		{ (Connection.South, Connection.West), 18 },
		{ (Connection.South, Connection.NorthWest), 19 },
		{ (Connection.South, Connection.SouthWest), 20 },
		{ (Connection.NorthEast, Connection.East), 21 },
		{ (Connection.East, Connection.SouthEast), 22 },
		{ (Connection.East, Connection.West), 23 },
		{ (Connection.East, Connection.NorthWest), 24 },
		{ (Connection.East, Connection.SouthWest), 25 },
		{ (Connection.NorthEast, Connection.SouthEast), 26 },
		{ (Connection.NorthEast, Connection.West), 27 },
		{ (Connection.NorthEast, Connection.NorthWest), 28 },
		{ (Connection.NorthEast, Connection.SouthWest), 29 },
		{ (Connection.SouthEast, Connection.West), 30 },
		{ (Connection.SouthEast, Connection.NorthWest), 31 },
		{ (Connection.SouthEast, Connection.SouthWest), 32 },
		{ (Connection.West, Connection.NorthWest), 33 },
		{ (Connection.SouthWest, Connection.West), 34 },
		{ (Connection.SouthWest, Connection.NorthWest), 35 },
	};

	private static(Connection, Connection) OrderedTuple(Connection directionA, Connection directionB)
	{
		return directionA < directionB ? (directionA, directionB) : (directionB, directionA);
	}

	public static int GetSpriteIndex(Connection directionA, Connection directionB, bool TRayScannerSprite = false)
	{
		(Connection, Connection) connectTuple = OrderedTuple(directionA, directionB);

		if (!LogicToIndexMap.TryGetValue(connectTuple, out int result))
		{
			Logger.Log($"WIRE DIRECTION NOT FOUND: {connectTuple.Item1} -> {connectTuple.Item2}");
			return 0;
		}

		// Wires viewed with a TRayScanner have their own variant sprites, offset from the normal ones
		return TRayScannerSprite ? result + 36 : result;
	}
}