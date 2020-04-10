using System.Collections.Generic;

public static class WireDirections
{
	private static Dictionary<string, int> LogicToIndexMap;

	public static int GetSpriteIndex(string logic)
	{
		if (LogicToIndexMap == null)
		{
			LogicToIndexMap = new Dictionary<string, int>();

			LogicToIndexMap.Add("North_Overlap", 0);
			LogicToIndexMap.Add("South_Overlap", 1);
			LogicToIndexMap.Add("East_Overlap", 2);
			LogicToIndexMap.Add("NorthEast_Overlap", 3);
			LogicToIndexMap.Add("SouthEast_Overlap", 4);
			LogicToIndexMap.Add("West_Overlap", 5);
			LogicToIndexMap.Add("NorthWest_Overlap", 6);
			LogicToIndexMap.Add("SouthWest_Overlap", 7);
			LogicToIndexMap.Add("North_South", 8);
			LogicToIndexMap.Add("North_East", 9);
			LogicToIndexMap.Add("North_NorthEast", 10);
			LogicToIndexMap.Add("North_SouthEast", 11);
			LogicToIndexMap.Add("North_West", 12);
			LogicToIndexMap.Add("North_NorthWest", 13);
			LogicToIndexMap.Add("North_SouthWest", 14);
			LogicToIndexMap.Add("East_South", 15);
			LogicToIndexMap.Add("NorthEast_South", 16);
			LogicToIndexMap.Add("SouthEast_South", 17);
			LogicToIndexMap.Add("South_West", 18);
			LogicToIndexMap.Add("South_NorthWest", 19);
			LogicToIndexMap.Add("South_SouthWest", 20);
			LogicToIndexMap.Add("NorthEast_East", 21);
			LogicToIndexMap.Add("East_SouthEast", 22);
			LogicToIndexMap.Add("East_West", 23);
			LogicToIndexMap.Add("East_NorthWest", 24);
			LogicToIndexMap.Add("East_SouthWest", 25);
			LogicToIndexMap.Add("NorthEast_SouthEast", 26);
			LogicToIndexMap.Add("NorthEast_West", 27);
			LogicToIndexMap.Add("NorthEast_NorthWest", 28);
			LogicToIndexMap.Add("NorthEast_SouthWest", 29);
			LogicToIndexMap.Add("SouthEast_West", 30);
			LogicToIndexMap.Add("SouthEast_NorthWest", 31);
			LogicToIndexMap.Add("SouthEast_SouthWest", 32);
			LogicToIndexMap.Add("West_NorthWest", 33);
			LogicToIndexMap.Add("SouthWest_West", 34);
			LogicToIndexMap.Add("SouthWest_NorthWest", 35);
		}

		if (!LogicToIndexMap.ContainsKey(logic))
		{
			Logger.Log("WIRE DIRECTION NOT FOUND: " + logic);
			return 0;
		}

		return LogicToIndexMap[logic];
	}
}