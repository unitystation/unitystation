using System;
using System.Collections.Generic;
using Chemistry;
using NaughtyAttributes;
using SecureStuff;
using UnityEngine;
using UnityEngine.Events;
using US13.Actions.V2;
using US13.HealthV2.Living;
using US13.HealthV2.Living.PolymorphicSystems;
using US13.HealthV2.Living.PolymorphicSystems.Bodypart;
using US13.Items;
using US13.Items.Traits;
using US13.Player;
using US13.Systems.Antagonists;
using US13.Systems.Antagonists.Objectives.TeamObjectives;

namespace US13.Systems.Antagonists
{
	public partial class VampireStageProgression : MonoBehaviour
	{
		[BoxGroup("Required References"), SerializeField] private SicknessReaction vampirismReaction = null;
		[BoxGroup("Required References"), SerializeField] private Reagent vampirismReagent = null;
		[BoxGroup("Required References"), SerializeField] private PreventCuredVampires preventCuredVampires = null;
		[BoxGroup("Required References"), SerializeField] private PlayerScript connectedPlayer;
		[BoxGroup("Required References"), SerializeField] private Antagonist vampireAntagonist = null;


		[SerializeField] private List<StageAbilities> stageAbilities = new List<StageAbilities>();
		private int currentVampirismStage = 0;

		private ReagentPoolSystem ReagentPool => connectedPlayer?.playerHealth?.reagentPoolSystem;

		[System.Serializable]
		private class StageAbilities
		{
			[field:SerializeField]
			public SerializableDictionary<ActionButtonData, SerializedAction> ActivatedAbilities { get; set; }
			public List<MutationSO> Mutations;
		}


		public void Apply(ReagentMix reagentMix)
		{
			if (ReagentPool == null) return;

			float diseaseAmount = reagentMix[vampirismReagent];
			int vampireStage = vampirismReaction.GetStageIDFromReagentAmount(ReagentPool, diseaseAmount) - 1;
			if(vampireStage == currentVampirismStage) return;
			if (vampireStage > currentVampirismStage) Evolve(vampireStage);
			else Devolve(vampireStage);
		}

		private void Devolve(int newStage)
		{
			if (connectedPlayer.Mind.AntagPublic.CurTeam == preventCuredVampires.Team)
			{
				preventCuredVampires.RemoveVampire(connectedPlayer.Mind);
				connectedPlayer.Mind.RemoveAntag();
			}
			for (int i = currentVampirismStage; i >= newStage; i--)
			{
				StageAbilities abilitiesToGain = stageAbilities[i];
				foreach (var action in abilitiesToGain.ActivatedAbilities)
				{
					connectedPlayer.Mind.PlayerButtonedActions?.ServerRemoveAction(action.Key.ID);
				}
				foreach (var mutation in abilitiesToGain.Mutations)
				{
					//Remove mutations
				}
			}

			currentVampirismStage = newStage;
		}

		private void Evolve(int newStage)
		{
			if (connectedPlayer.Mind.AntagPublic.CurTeam != preventCuredVampires.Team)
			{
				preventCuredVampires.AddNewVampire(connectedPlayer.Mind);
				connectedPlayer.Mind.InitAntag(vampireAntagonist, null);
			}

			for (int i = currentVampirismStage; i <= newStage; i++)
			{
				StageAbilities abilitiesToGain = stageAbilities[i];
				foreach (var action in abilitiesToGain.ActivatedAbilities)
				{
					connectedPlayer.Mind.PlayerButtonedActions?.ServerAddAction(action.Key, action.Value.Invoke);
				}
				foreach (var mutation in abilitiesToGain.Mutations)
				{
					//Add mutations
				}

			}

			currentVampirismStage = newStage;

		}


	}
}
