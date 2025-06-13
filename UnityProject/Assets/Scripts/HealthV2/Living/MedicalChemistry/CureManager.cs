using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Chemistry;
using Messages.Server;
using Shared.Managers;
using UnityEngine;

namespace HealthV2.Sickness
{
	public class CureManager : SingletonManager<CureManager>
	{
		[System.Serializable]
		public struct CureableSickness
		{
			public Reagent Sickness;
			public Reaction CureReaction;

			public int NumberOfCluesForSickness;
			public List<Reagent> PossibleCureReagents;
		}

		[System.Serializable]
		public struct Cure
		{
			public Reagent[] ClueReagents;
			public Reagent CureReagentA;
			public Reagent CureReagentB;
			public Reagent InhibitorReagentA;
			public Reagent InhibitorReagentB;
		}

		public List<CureableSickness> CureableSicknesses = new List<CureableSickness>();

		public static Dictionary<Reagent, Cure> InitialisedSicknesses { get; private set; } = new Dictionary<Reagent, Cure>();

		private void OnEnable()
		{
			EventManager.AddHandler(Event.ScenesLoadedServer, RandomiseCureData);
		}

		private void OnDisable()
		{
			EventManager.RemoveHandler(Event.ScenesLoadedServer, RandomiseCureData);
		}


		public void RandomiseCureData()
		{
			CleanUpPastCures();

			InitialisedSicknesses = new();

			CureReactionSyncMessage.CureDataMessage data;

			foreach (var cureableSickness in CureableSicknesses)
			{
				Cure cure = new Cure();
				GetCluesForRound(ref cure, cureableSickness);

				InitialisedSicknesses.Add(cureableSickness.Sickness, cure); //This dictionary is used later to get the clues and cures in the virology machines

				//This gets all the indexes that will be used to sync the chemical reactions to the client
				data.SicknessReagentIndex = cureableSickness.Sickness.IndexInSingleton;
				data.CureReactionIndex = cureableSickness.CureReaction.IndexInSingleton;

				data.CureIngredientA = cure.CureReagentA.IndexInSingleton;
				data.CureIngredientB = cure.CureReagentB.IndexInSingleton;
				data.CureInhibitorA = cure.InhibitorReagentA.IndexInSingleton;
				data.CureInhibitorB = cure.InhibitorReagentB.IndexInSingleton;

				CureReactionSyncMessage.SendToAll(data);
			}
		}

		/// <summary>
		/// This function removes the cure reaction from the RelatedReactions array of the current cure ingredients,
		/// This works off the assumption as cure reactions are runtime, they will always be the last elements in this array.
		/// </summary>
		private void CleanUpPastCures()
		{
			if (InitialisedSicknesses.Count == 0) return;

			foreach (CureableSickness sickness in CureableSicknesses)
			{
				int reactionAmount = InitialisedSicknesses[sickness.Sickness].CureReagentA.RelatedReactions.Length;

				InitialisedSicknesses[sickness.Sickness].CureReagentA.RelatedReactions =
					InitialisedSicknesses[sickness.Sickness].CureReagentA.RelatedReactions.Take(reactionAmount - 1)
						.ToArray();

				reactionAmount = InitialisedSicknesses[sickness.Sickness].CureReagentB.RelatedReactions.Length;

				InitialisedSicknesses[sickness.Sickness].CureReagentB.RelatedReactions =
					InitialisedSicknesses[sickness.Sickness].CureReagentB.RelatedReactions.Take(reactionAmount - 1)
						.ToArray();
			}
		}

		private void GetCluesForRound(ref Cure cure, in CureableSickness cureableSickness)
		{
			List<Reagent> possibleCureReagents = new List<Reagent>(cureableSickness.PossibleCureReagents);

			cure.ClueReagents = new Reagent[cureableSickness.NumberOfCluesForSickness];
			for (int i = 0; i < cureableSickness.NumberOfCluesForSickness; i++)
			{
				cure.ClueReagents[i] = PickAndRemoveRandomReagent(possibleCureReagents); //Picks 5 random reagents with no duplicates
			}

			cure.InhibitorReagentA = cure.ClueReagents[0];
			cure.InhibitorReagentB = cure.ClueReagents[1];
			cure.CureReagentA = cure.ClueReagents[2];
			cure.CureReagentB = cure.ClueReagents[3];
		}

		private Reagent PickAndRemoveRandomReagent(in List<Reagent> possibleReagents)
		{
			var reagent = possibleReagents.PickRandom();
			possibleReagents.Remove(reagent);
			return reagent;
		}
	}
}