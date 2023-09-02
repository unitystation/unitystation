
using UnityEngine;
using Mirror;
using System.Linq;
using SecureStuff;
using System.Collections.Generic;
using Logs;

namespace Antagonists
{
	public class CodeWordManager : NetworkBehaviour
	{
		public readonly SyncList<string> Words = new SyncList<string>();
		public readonly SyncList<string> Responses = new SyncList<string>();

		[field: SerializeField] public List<JobType> CodeWordRoles { get; private set; } = new List<JobType>();

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
			EventManager.AddHandler(Event.ScenesLoadedServer, ChooseCodeWords);
		}

		private void OnDisable()
		{
			EventManager.RemoveHandler(Event.ScenesLoadedServer, ChooseCodeWords);
		}

		[Server]
		public void ChooseCodeWords()
		{
			if (CustomNetworkManager.IsHeadless == false && CustomNetworkManager.IsServer == false) return;

			Words.Clear();
			Responses.Clear();

			string filePath =  "TraitorCodeWords.txt";

			if(AccessFile.Exists(filePath) == false)
			{
				Loggy.LogError($"Traitor Code Words: Could not find text file to read at: {filePath}");
				return;
			}

			string[] allWords = AccessFile.ReadAllLines(filePath);

			allWords = allWords.Shuffle().ToArray();

			for (int i = 0; i < WORD_COUNT; i++)
			{
				Words.Add(allWords[i]);
				Responses.Add(allWords[WORD_COUNT + i]);
			}

			netIdentity.isDirty = true;
		}
	}
}
