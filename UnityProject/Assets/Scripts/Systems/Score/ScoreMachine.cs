using System;
using System.Collections.Generic;
using Logs;
using Shared.Managers;
using UnityEngine;
using UnityEngine.Events;

namespace Systems.Score
{
	public class ScoreMachine : SingletonManager<ScoreMachine>
	{
		public Dictionary<string, ScoreEntry> Scores { get; private set; }
		public UnityEvent<string, int> OnScoreChanged;

		public enum ScoreType
		{
			Int,
			Bool,
			String,
		}

		void ClearScores()
		{
			Scores.Clear();
			OnScoreChanged?.RemoveAllListeners();
		}

		public override void Awake()
		{
			base.Awake();
			EventManager.AddHandler(Event.PreRoundStarted, ClearScores);
			Scores = new Dictionary<string, ScoreEntry>(); //This only works if set on Awake()
		}

		public override void OnDestroy()
		{
			EventManager.RemoveHandler(Event.PreRoundStarted, ClearScores);
			base.OnDestroy();
		}

		/// <summary>
		/// Used to add a new entry to the score machine to keep track of a subject. Make sure the ID is unique and saved somewhere!!!
		/// </summary>
		/// <param name="ID">The unique ID of the score entry. Used to grab and manipulate the entry from scores dictionary.</param>
		/// <param name="scoreName">The name of the score shown on UIs</param>
		/// <param name="type">Are you tracking a number? bool? or string?</param>
		/// <param name="category">What category does this score fall under? (MiscScore does not appear on round end UI)</param>
		/// <param name="alignment">Is entry considered a negative thing? a good thing? a weird thing? or do you not want to specify it's alignment?</param>
		public static void AddNewScoreEntry(string ID, string scoreName, ScoreType type, ScoreCategory category = ScoreCategory.MiscScore, ScoreAlignment alignment = ScoreAlignment.Unspecified)
		{
			if (Instance.Scores.ContainsKey(ID))
			{
				return;
			}
			switch (type)
			{
				case ScoreType.Int:
					ScoreEntryInt newEntryInt = new ScoreEntryInt
					{
						ScoreName = scoreName,
						Category = category,
						Alignment = alignment
					};
					Instance.Scores.Add(ID, newEntryInt);
					break;
				case ScoreType.Bool:
					ScoreEntryBool newEntryBool = new ScoreEntryBool
					{
						ScoreName = scoreName,
						Category = category,
						Alignment = alignment
					};
					Instance.Scores.Add(ID, newEntryBool);
					break;
				case ScoreType.String:
					ScoreEntryString newEntryString = new ScoreEntryString
					{
						ScoreName = scoreName,
						Category = category,
						Alignment = alignment
					};
					Instance.Scores.Add(ID, newEntryString);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, null);
			}
		}

		/// <summary>
		/// How much do you want to add or subtract from a score? (Use negative numbers to subtract from the score.)
		/// </summary>
		public static void AddToScoreInt(int valueToAddOnTop, string ID)
		{
			if (Instance.Scores.ContainsKey(ID) == false)
			{
				Loggy.LogError($"[ScoreMachine] - {ID} does not exist in the score machine!");
				return;
			}

			if (Instance.Scores[ID] is not ScoreEntryInt c)
			{
				Loggy.LogError($"[ScoreMachine] - Attempted to add an integer to {ID} but it's entry is not a ScoreEntryInt!");
				return;
			}
			c.Score += valueToAddOnTop;
			Instance.OnScoreChanged?.Invoke(ID, valueToAddOnTop);
		}

		/// <summary>
		/// whats the new bool for this score?
		/// </summary>
		public static void AddToScoreBool(bool newValue, string ID)
		{
			if (Instance.Scores.ContainsKey(ID) == false)
			{
				Loggy.LogError($"[ScoreMachine] - {ID} does not exist in the score machine!");
				return;
			}

			if (Instance.Scores[ID] is not ScoreEntryBool c)
			{
				Loggy.LogError($"[ScoreMachine] - Attempted to change a bool in {ID} but it's entry is not a ScoreEntryBool!");
				return;
			}
			c.Score = newValue;
		}

		/// <summary>
		/// whats the new string for this score?
		/// </summary>
		public static void AddToScoreString(String newValue, int givenScore, string ID)
		{
			if (Instance.Scores.ContainsKey(ID) == false)
			{
				Loggy.LogError($"[ScoreMachine] - {ID} does not exist in the score machine!");
				return;
			}

			if (Instance.Scores[ID] is not ScoreEntryString c)
			{
				Loggy.LogError($"[ScoreMachine] - Attempted to change a string in {ID} but it's entry is not a ScoreEntryString!");
				return;
			}
			c.Score = newValue;
		}

		/// <summary>
		/// Returns the int score winner from a select number of entries.
		/// </summary>
		public string ScoreIntWinner(List<string> IDs)
		{
			var highestScoreIndex = -1;
			var winner = "";
			foreach (var id in IDs)
			{
				if (Scores.ContainsKey(id) == false)
				{
					Loggy.LogError($"[ScoreMachine] - {id} does not exist in the score machine!");
					continue;
				}
				if (Scores[id] is not ScoreEntryInt c || c.Score <= highestScoreIndex) continue;
				highestScoreIndex = c.Score;
				winner = id;
			}

			return winner;
		}

		/// <summary>
		/// Returns an entry that has the highest int score from the ScoreMachine score dictionary.
		/// </summary>
		/// <returns></returns>
		public string ScoreIntWinner()
		{
			var highestScoreIndex = -1;
			var winner = "";
			foreach (var id in Scores.Keys)
			{
				if (Scores[id] is not ScoreEntryInt c || c.Score <= highestScoreIndex) continue;
				highestScoreIndex = c.Score;
				winner = id;
			}

			return winner;
		}

		public static String ScoreTypeResultAsString(ScoreEntry entry)
		{
			return entry switch
			{
				ScoreEntryInt integerScore => integerScore.Score.ToString(),
				ScoreEntryBool booleanScore => booleanScore.Score.ToString(),
				ScoreEntryString stringScore => stringScore.Score,
				_ => null
			};
		}
	}
}