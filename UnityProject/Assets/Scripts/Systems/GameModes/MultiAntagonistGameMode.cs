using System.Collections.Generic;
using System.Linq;
using Antagonists;
using GameModes;
using JetBrains.Annotations;
using Logs;
using NaughtyAttributes;
using Player;
using UnityEngine;

[CreateAssetMenu(menuName="ScriptableObjects/GameModes/MultiAntagonistGameMode")]
public class MultiAntagonistGameMode : GameMode
{

	public List<AntagonistData> AntagonistDatas = new List<AntagonistData>();

	[System.Serializable]
	public class AntagonistData
	{
		public float ratio;
		public Antagonist Antagonist;
		public bool UniqueOne;
		[ShowIf(nameof(UniqueOne))] public float OneTimechance;

		public int MinPop;
	}

	private List<AntagonistData> ReuseList = new List<AntagonistData>();

	protected override Antagonist PickAndCheckAntagonist(PlayerInfo PlayerInfo, [CanBeNull] PlayerSpawnRequest spawnRequest)
	{
		ReuseList.Clear();

		int players = PlayerList.Instance.ReadyPlayers.Count;

		double cumulativeTotal = 0.0;

		var CharacterSheet = PlayerInfo.RequestedCharacterSettings;

		if (spawnRequest != null)
		{
			CharacterSheet = spawnRequest.CharacterSettings;
		}


		foreach (var Antag in AntagonistDatas)
		{
			if (players < Antag.MinPop) continue;
			if (Antag.UniqueOne)
			{

				if (AntagManager.Instance.TriedAntagonists.Contains(Antag))
				{
					continue;
				}

				if (HasAntagEnabled(ref CharacterSheet.AntagPreferences, Antag.Antagonist) == false
				    || PlayerList.Instance.IsJobBanned(PlayerInfo.AccountId, Antag.Antagonist.AntagJobType))
				{
					continue;
				}

				AntagManager.Instance.TriedAntagonists.Add(Antag);

				if (RNG.RoleChance(Antag.OneTimechance) == false)
				{
					continue; //Failed role
				}

				ReuseList.Clear();
				ReuseList.Add(Antag);
				cumulativeTotal = 1;
				break;
			}
			ReuseList.Add(Antag);
			cumulativeTotal += Antag.ratio;
		}

		double randomValue = RNG.Random.NextDouble() * cumulativeTotal; // Random value between 0 and 1
		double cumulative = 0.0;

		if (ReuseList.Count == 0) return null;
		AntagonistData Antagonist =  ReuseList[0];

		if (ReuseList.Count != 1)
		{
			foreach (var item in ReuseList)
			{
				cumulative += item.ratio;
				if (randomValue <= cumulative)
				{
					Antagonist = item;
				}
			}
		}

		if (HasAntagEnabled(ref CharacterSheet.AntagPreferences, Antagonist.Antagonist) == false
		    || PlayerList.Instance.IsJobBanned(PlayerInfo.AccountId, Antagonist.Antagonist.AntagJobType))
		{
			return null;
		}

		if (AllocateJobsToAntags == false && Antagonist.Antagonist.AntagOccupation == null)
		{
			Loggy.Error().Format("AllocateJobsToAntags is false but {0} AntagOccupation is null! " +
			                     "Game mode must either set AllocateJobsToAntags or possible antags neeed an AntagOccupation.",
				Category.Antags, Antagonist.Antagonist.AntagName);
			return null;
		}

		return Antagonist.Antagonist;
	}

	/*/
TODO Update functions with new versions
HasPossibleAntagEnabled <<<
HasPossibleAntagNotBanned <<<
	 /*/



}

/*/
Traitor, blood brothers, change ling, swapper

= usual fraction
Traitor = 75
blood brothers if (greater than 15) = 25
swapper  if (greater than 20) and 50% chance = guaranteed level
changeling if (greater than 30) and 50% chance = guaranteed level
/*/