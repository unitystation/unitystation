using System.Collections.Generic;

namespace Wiring
{
	public static class WireDirections
	{
		private static Dictionary<string, int> LogicToIndexMap;

		public static int GetSpriteIndex(string logic)
		{
			if (LogicToIndexMap == null)
			{
				LogicToIndexMap = new Dictionary<string, int>();

				LogicToIndexMap.Add("1", 0);
				LogicToIndexMap.Add("2", 1);
				LogicToIndexMap.Add("4", 2);
				LogicToIndexMap.Add("5", 3);
				LogicToIndexMap.Add("6", 4);
				LogicToIndexMap.Add("8", 5);
				LogicToIndexMap.Add("9", 6);
				LogicToIndexMap.Add("10", 7);
				LogicToIndexMap.Add("1_2", 8);
				LogicToIndexMap.Add("1_4", 9);
				LogicToIndexMap.Add("1_5", 10);
				LogicToIndexMap.Add("1_6", 11);
				LogicToIndexMap.Add("1_8", 12);
				LogicToIndexMap.Add("1_9", 13);
				LogicToIndexMap.Add("1_10", 14);
				LogicToIndexMap.Add("2_4", 15);
				LogicToIndexMap.Add("2_5", 16);
				LogicToIndexMap.Add("2_6", 17);
				LogicToIndexMap.Add("2_8", 18);
				LogicToIndexMap.Add("2_9", 19);
				LogicToIndexMap.Add("2_10", 20);
				LogicToIndexMap.Add("4_5", 21);
				LogicToIndexMap.Add("4_6", 22);
				LogicToIndexMap.Add("4_8", 23);
				LogicToIndexMap.Add("4_9", 24);
				LogicToIndexMap.Add("4_10", 25);
				LogicToIndexMap.Add("5_6", 26);
				LogicToIndexMap.Add("5_8", 27);
				LogicToIndexMap.Add("5_9", 28);
				LogicToIndexMap.Add("5_10", 29);
				LogicToIndexMap.Add("6_8", 30);
				LogicToIndexMap.Add("6_9", 31);
				LogicToIndexMap.Add("6_10", 32);
				LogicToIndexMap.Add("8_9", 33);
				LogicToIndexMap.Add("8_10", 34);
				LogicToIndexMap.Add("9_10", 35);
			}
			return LogicToIndexMap[logic];
		}
	}
}