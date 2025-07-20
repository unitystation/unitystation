using System.Linq;
using Chemistry;
using HealthV2.Sickness;
using Logs;
using Messages.Client;
using Mirror;
using ScriptableObjects;

namespace Messages.Server
{
	/// <summary>
	/// Used to sync the ingredients and inhibitors of a cure reaction on round start
	/// </summary>
	public class CureReactionSyncMessage : ServerMessage<CureReactionSyncMessage.CureDataMessage>
	{
		public struct CureDataMessage : NetworkMessage
		{
			//The reagent indexes of the two chemicals that will constitute this cure
			public int CureIngredientA;
			public int CureIngredientB;

			//The reagent indexes of the two chemicals that will inhibit this cure
			public int CureInhibitorA;
			public int CureInhibitorB;

			public int CureReactionIndex;
			public int SicknessReagentIndex;
		}

		public override void Process(CureDataMessage msg)
		{
			if (ChemistryReagentsSO.Instance == null) return;

			CureManager.Instance.CleanUpPastCures();

			int reactionIndex = msg.CureReactionIndex;
			if (reactionIndex < 0 || reactionIndex >= ChemistryReagentsSO.Instance.AllChemistryReactions.Count) return;

			Reaction cureReaction = ChemistryReagentsSO.Instance.AllChemistryReactions[msg.CureReactionIndex];

			cureReaction.ingredients = new();
			cureReaction.catalysts = new();
			cureReaction.inhibitors = new();
			cureReaction.results = new();

			SetInhibitor(msg.CureInhibitorA, cureReaction);
			SetInhibitor(msg.CureInhibitorB, cureReaction);
			SetIngredient(msg.CureIngredientA, 1, cureReaction);
			SetIngredient(msg.CureIngredientB, 1, cureReaction);
			SetIngredient(msg.SicknessReagentIndex, 5, cureReaction);
		}

		private void SetInhibitor(int reagentIndex, in Reaction reactionToEffect)
		{
			Reagent reagent;
			if (FetchReagent(reagentIndex, out reagent) == false)
			{
				Loggy.Error(
					$"[CureReactionSyncMessage/Process]: Attempted to set inhibitor reagent for {reactionToEffect.name} but the reagent index was outside the bounds of the array!");
				return;
			}

			reactionToEffect.inhibitors.Add(reagent, 1);
		}

		private void SetIngredient(int reagentIndex, int usedAmount, in Reaction reactionToEffect)
		{
			Reagent reagent;
			if (FetchReagent(reagentIndex, out reagent) == false)
			{
				Loggy.Error(
					$"[CureReactionSyncMessage/SetIngredient]: Attempted to set ingredient reagent for {reactionToEffect.name} but the reagent index was outside the bounds of the array!");
				return;
			}
			reactionToEffect.ingredients.Add(reagent, usedAmount);

			//Add the cure reaction to related reactions if it isn't already
			if(reagent.RelatedReactions.Contains(reactionToEffect) == false) reagent.RelatedReactions = reagent.RelatedReactions.Append(reactionToEffect).ToArray();
		}

		private bool FetchReagent(int reagentIndex, out Reagent reagent)
		{
			reagent = null;
			if (reagentIndex < 0 || reagentIndex >= ChemistryReagentsSO.Instance.AllChemistryReagents.Count) return false;
			reagent = ChemistryReagentsSO.Instance.AllChemistryReagents[reagentIndex];

			return true;
		}
	}

	public class RequestCureSyncMessage : ClientMessage<RequestCureSyncMessage.SyncDataMessage>
	{
		public struct SyncDataMessage : NetworkMessage { }

		public override void Process(SyncDataMessage msg)
		{
			if (GameManager.Instance.CurrentRoundState != RoundState.Started) return;
			CureManager.Instance.SynchroniseClient(this.SentBy);
		}
	}
}
