using MiniGames.MiniGameModules;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjects.MiniGames
{
	[CreateAssetMenu(fileName = "RGPuzzleList", menuName = "ScriptableObjects/MiniGames/ReflectionGoldPuzzleList")]
	public class ReflectionGolfPuzzleList : ScriptableObject
	{
		[SerializeField]
		private List<string> easyLevelNames = new List<string>();
		[SerializeField]
		private List<string> normalLevelNames = new List<string>();
		[SerializeField]
		private List<string> hardLevelNames = new List<string>();
		[SerializeField]
		private List<string> veryHardLevelNames = new List<string>();

		private const int LOWER_BIAS = 3;
		private const int HIGHER_BIAS = 6;
		private const int AT_LEVEL_BIAS = 10;

		/// <summary>
		/// Returns the string name of a level picked at random biased by the selected difficulty.
		/// </summary>
		/// <param name="difficulty">The selected difficulty</param>
		/// <returns>String: name of the selected level file</returns>
		public string RetrieveLevel(Difficulty difficulty)
		{
			int ran = 0;
			switch(difficulty)
			{
				case Difficulty.Easy:
					ran = Random.Range(0, AT_LEVEL_BIAS + HIGHER_BIAS);
					if (ran < AT_LEVEL_BIAS) return easyLevelNames.PickRandom();
					else return normalLevelNames.PickRandom();
				case Difficulty.Normal:
					ran = Random.Range(0, LOWER_BIAS + AT_LEVEL_BIAS + HIGHER_BIAS);
					if (ran < LOWER_BIAS) return easyLevelNames.PickRandom();
					else if(ran < LOWER_BIAS + AT_LEVEL_BIAS) return normalLevelNames.PickRandom();
					else return hardLevelNames.PickRandom();
				case Difficulty.Hard:
					ran = Random.Range(0, LOWER_BIAS + AT_LEVEL_BIAS + HIGHER_BIAS);
					if (ran < LOWER_BIAS) return normalLevelNames.PickRandom();
					else if (ran < LOWER_BIAS + AT_LEVEL_BIAS) return hardLevelNames.PickRandom();
					else return veryHardLevelNames.PickRandom();
				case Difficulty.VeryHard:
					ran = Random.Range(0, AT_LEVEL_BIAS + LOWER_BIAS);
					if (ran < LOWER_BIAS) return hardLevelNames.PickRandom();
					else return veryHardLevelNames.PickRandom();
				default:
					return normalLevelNames.PickRandom();
			}
		}
	}
}
