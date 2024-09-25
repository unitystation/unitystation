using System;
using Logs;
using SecureStuff;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace MiniGames.MiniGameModules
{
	public class ReflectionGolfLevelImporter
	{
		private const int LOWER_BIAS = 6;
		private const int HIGHER_BIAS = 4;
		private const int AT_LEVEL_BIAS = 8;

		private static List<string> easyLevelNames = new List<string>();
		private static List<string> normalLevelNames = new List<string>();
		private static List<string> hardLevelNames = new List<string>();
		private static List<string> veryHardLevelNames = new List<string>();

		private static bool isInitialised = false;

		public static string PickLevel(Difficulty difficulty)
		{
			if (isInitialised == false && Import() == false) return "";
			
			int ran = 0;
			switch (difficulty)
			{
				case Difficulty.Easy:
					ran = UnityEngine.Random.Range(0, AT_LEVEL_BIAS + HIGHER_BIAS);
					if (ran < AT_LEVEL_BIAS) return easyLevelNames.PickRandom();
					else return normalLevelNames.PickRandom();
				case Difficulty.Normal:
					ran = UnityEngine.Random.Range(0, LOWER_BIAS + AT_LEVEL_BIAS + HIGHER_BIAS);
					if (ran < LOWER_BIAS) return easyLevelNames.PickRandom();
					else if (ran < LOWER_BIAS + AT_LEVEL_BIAS) return normalLevelNames.PickRandom();
					else return hardLevelNames.PickRandom();
				case Difficulty.Hard:
					ran = UnityEngine.Random.Range(0, LOWER_BIAS + AT_LEVEL_BIAS + HIGHER_BIAS);
					if (ran < LOWER_BIAS) return normalLevelNames.PickRandom();
					else if (ran < LOWER_BIAS + AT_LEVEL_BIAS) return hardLevelNames.PickRandom();
					else return veryHardLevelNames.PickRandom();
				case Difficulty.VeryHard:
					ran = UnityEngine.Random.Range(0, AT_LEVEL_BIAS + LOWER_BIAS);
					if (ran < LOWER_BIAS) return hardLevelNames.PickRandom();
					else return veryHardLevelNames.PickRandom();
				default:
					return normalLevelNames.PickRandom();
			}

		}

		public static bool Import()
		{
			string path = Path.Combine("MiniGamesData", $"MiniGame_Levels_ReflectionGolf.json");

			if (AccessFile.Exists(path) == false)
			{
				Loggy.LogError($"MiniGames/MiniGameLevelImporter.cs at line 79. The specified file path does not exist! {path}");
				return false;
			}

			string json = AccessFile.Load(path);
			if (json == null || json.Length < 3) return false;
			var JsonLevelConfig = JsonConvert.DeserializeObject<Dictionary<String, System.Object>>(json);

			easyLevelNames = JsonConvert.DeserializeObject<List<string>>(JsonLevelConfig["EasyLevels"].ToString());
			normalLevelNames = JsonConvert.DeserializeObject<List<string>>(JsonLevelConfig["MediumLevels"].ToString());
			hardLevelNames = JsonConvert.DeserializeObject<List<string>>(JsonLevelConfig["HardLevels"].ToString());
			veryHardLevelNames = JsonConvert.DeserializeObject<List<string>>(JsonLevelConfig["VeryHardLevels"].ToString());

			isInitialised = true;
			return true;
		}
	}
}
