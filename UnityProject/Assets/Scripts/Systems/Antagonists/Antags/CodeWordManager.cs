using System.IO;
using UnityEngine;
using Mirror;

namespace Antagonists
{
	public class CodeWordManager : NetworkBehaviour
	{
		public readonly SyncList<string> Words = new SyncList<string>();
		public readonly SyncList<string> Responses = new SyncList<string>();

		public const int WORD_COUNT = 3;

		public static CodeWordManager Instance;

		public void Awake()
		{
			if (Instance == null) Instance = this;
			else
			{
				Destroy(gameObject);
			}
		}

		private void OnEnable()
		{
			EventManager.AddHandler(Event.RoundStarted, ChooseCodeWords);
		}

		private void OnDisable()
		{
			EventManager.RemoveHandler(Event.RoundStarted, ChooseCodeWords);
		}

		public void ChooseCodeWords()
		{
			Words.Clear();
			Responses.Clear();

			string filePath = Path.Combine(Application.streamingAssetsPath, "TraitorCodeWords.txt");

			if(File.Exists(filePath) == false)
			{
				Logger.LogError($"Traitor Code Words: Could not find text file to read at: {filePath}");
				return;
			}

			string[] allWords = File.ReadAllLines(filePath);

			for(int i = 0; i < WORD_COUNT; i++)
			{
				Words.Add(allWords[Random.Range(0, allWords.Length)]);
				Responses.Add(allWords[Random.Range(0, allWords.Length)]);
			}

			netIdentity.isDirty = true;
		}
	}
}
