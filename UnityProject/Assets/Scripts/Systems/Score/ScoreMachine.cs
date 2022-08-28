using System;
using System.Collections.Generic;
using Shared.Managers;

namespace Systems.Score
{
	public class ScoreMachine : SingletonManager<ScoreMachine>
	{
		public Dictionary<string, ScoreEntry> Scores { get; private set; }

		public enum ScoreType
		{
			Int,
			Bool,
			String,
		}

		public override void Awake()
		{
			base.Awake();
			EventManager.AddHandler(Event.RoundStarted, () => Scores.Clear());
		}

		/// <summary>
		/// Used to add a new entry to the score machine to keep track of a subject. Make sure the ID is unique and saved somewhere!!!
		/// </summary>
		/// <param name="ID">The unique ID of the score entry. Used to grab and manipulate the entry from scores dictionary.</param>
		/// <param name="scoreName">The name of the score shown on UIs</param>
		/// <param name="type">Are you tracking a number? bool? or string?</param>
		/// <param name="category">What category does this score fall under? (MiscScore does not appear on round end UI)</param>
		public void AddNewScoreEntry(string ID, string scoreName, ScoreType type, ScoreCategory category = ScoreCategory.MiscScore)
		{
			switch (type)
			{
				case ScoreType.Int:
					ScoreEntryInt newEntryInt = new ScoreEntryInt
					{
						ScoreName = scoreName,
						Category = category
					};
					Scores.Add(ID, newEntryInt);
					break;
				case ScoreType.Bool:
					ScoreEntryBool newEntryBool = new ScoreEntryBool
					{
						ScoreName = scoreName,
						Category = category
					};
					Scores.Add(ID, newEntryBool);
					break;
				case ScoreType.String:
					ScoreEntryString newEntryString = new ScoreEntryString
					{
						ScoreName = scoreName,
						Category = category
					};
					Scores.Add(ID, newEntryString);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, null);
			}
		}

		/// <summary>
		/// How much do you want to add or subtract from a score? (Use negative numbers to subtract from the score.)
		/// </summary>
		public void AddToScoreInt(int valueToAddOnTop, string ID)
		{
			if (Scores.ContainsKey(ID) == false)
			{
				Logger.LogError($"{ID} does not exist in the score machine!");
				return;
			}

			if (Scores[ID] is not ScoreEntryInt c)
			{
				Logger.LogError($"Attempted to add an integer to {ID} but it's entry is not a ScoreEntryInt!");
				return;
			}
			c.Score += valueToAddOnTop;
		}

		/// <summary>
		/// whats the new bool for this score?
		/// </summary>
		public void AddToScoreBool(bool newValue, string ID)
		{
			if (Scores.ContainsKey(ID) == false)
			{
				Logger.LogError($"{ID} does not exist in the score machine!");
				return;
			}

			if (Scores[ID] is not ScoreEntryBool c)
			{
				Logger.LogError($"Attempted to change a bool in {ID} but it's entry is not a ScoreEntryBool!");
				return;
			}
			c.Score = newValue;
		}

		/// <summary>
		/// whats the new string for this score?
		/// </summary>
		public void AddToScoreString(String newValue, string ID)
		{
			if (Scores.ContainsKey(ID) == false)
			{
				Logger.LogError($"{ID} does not exist in the score machine!");
				return;
			}

			if (Scores[ID] is not ScoreEntryString c)
			{
				Logger.LogError($"Attempted to change a string in {ID} but it's entry is not a ScoreEntryString!");
				return;
			}
			c.Score = newValue;
		}

		public string ScoreIntWinner(List<string> IDs)
		{
			var highestScoreIndex = -1;
			var winner = "";
			foreach (var id in IDs)
			{
				if (Scores.ContainsKey(id) == false)
				{
					Logger.LogError($"{id} does not exist in the score machine!");
					continue;
				}
				if (Scores[id] is not ScoreEntryInt c || c.Score <= highestScoreIndex) continue;
				highestScoreIndex = c.Score;
				winner = id;
			}

			return winner;
		}
	}
}