using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Chemistry;
using Logs;
using Messages.Server;
using Mirror;
using Player;
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

		public override void Awake()
		{
			base.Awake();
			if (CustomNetworkManager.IsServer == false) JoinedViewer.DelayTillAuthenticated.Add(RequestCureData);
			EventManager.AddHandler(Event.RoundStarted, RandomiseCureData);
		}

		public override void OnDestroy()
		{
			EventManager.RemoveHandler(Event.RoundStarted, RandomiseCureData);
			base.OnDestroy();
		}

		/// <summary>
		/// Ensures late join clients receive the updated cure data
		/// </summary>
		private void RequestCureData()
		{
			if (CustomNetworkManager.IsServer) return;

			RequestCureSyncMessage.SyncDataMessage data = new();
			RequestCureSyncMessage.Send(data);
		}


		public void RandomiseCureData()
		{
			if (CustomNetworkManager.IsServer == false) return;

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

			Chat.AddGameWideSystemMsgToChat($"<color=green>Randomised cures for round, registering {InitialisedSicknesses.Count} sicknesses</color>");
		}

		/// <summary>
		/// Used for late join clients to sync them to the current cure states.
		/// </summary>
		/// <param name="client">The network connection for the late join client</param>
		public void SynchroniseClient(NetworkConnection client)
		{
			CureReactionSyncMessage.CureDataMessage data;
			foreach (var cureableSickness in CureableSicknesses)
			{
				if (InitialisedSicknesses.TryGetValue(cureableSickness.Sickness, out var cure) == false) continue;

				//This gets all the indexes that will be used to sync the chemical reactions to the client
				data.SicknessReagentIndex = cureableSickness.Sickness.IndexInSingleton;
				data.CureReactionIndex = cureableSickness.CureReaction.IndexInSingleton;

				data.CureIngredientA = cure.CureReagentA.IndexInSingleton;
				data.CureIngredientB = cure.CureReagentB.IndexInSingleton;
				data.CureInhibitorA = cure.InhibitorReagentA.IndexInSingleton;
				data.CureInhibitorB = cure.InhibitorReagentB.IndexInSingleton;

				CureReactionSyncMessage.SendTo(client, data);
			}
		}

		/// <summary>
		/// This function removes the cure reaction from the RelatedReactions array of the current cure ingredients,
		/// This works off the assumption as cure reactions are runtime, they will always be the last elements in this array.
		/// </summary>
		public void CleanUpPastCures()
		{
			if (InitialisedSicknesses.Count == 0) return;

			foreach (CureableSickness sickness in CureableSicknesses)
			{
				var reagentA = InitialisedSicknesses[sickness.Sickness].CureReagentA;
				reagentA.RelatedReactions =
					reagentA.RelatedReactions
						.Where(r => r is SicknessCureReaction == false)
						.ToArray();

				var reagentB = InitialisedSicknesses[sickness.Sickness].CureReagentB;
				reagentB.RelatedReactions =
					reagentB.RelatedReactions
						.Where(r => r is SicknessCureReaction == false)
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