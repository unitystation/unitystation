using System.IO;
using UnityEngine;

namespace Antagonists
{
	public static class CodeWords
	{
		public static string[] Words { get; private set; } = new string[WORD_COUNT];
		public static string[] Responses { get; private set; } = new string[WORD_COUNT];

		public const int WORD_COUNT = 3;

		public static bool ChooseCodeWords()
		{
			string filePath = Path.Combine(Application.streamingAssetsPath, "TraitorCodeWords.txt");

			if(File.Exists(filePath) == false)
			{
				Logger.LogError($"Traitor Code Words: Could not find text file to read at: {filePath}");
				return false;
			}

			string[] allWords = File.ReadAllLines(filePath);

			for(int i = 0; i < WORD_COUNT; i++)
			{
				Words[i] = allWords[Random.Range(0, allWords.Length)];
				Responses[i] = allWords[Random.Range(0, allWords.Length)];
			}
	
			return true;
		}
	}
}
