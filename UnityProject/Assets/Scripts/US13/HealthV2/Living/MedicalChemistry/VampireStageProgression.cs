using System;
using System.Collections.Generic;
using Chemistry;
using UnityEngine;
using US13.Actions.V2;
using US13.HealthV2.Living.PolymorphicSystems.Bodypart;
using US13.Items;
using US13.Items.Traits;
using US13.Player;
using US13.Systems.Antagonists;
using US13.Systems.Antagonists.Objectives.TeamObjectives;

namespace US13.HealthV2.Living.MedicalChemistry
{
	[CreateAssetMenu(fileName = "vampireStageProgression",
		menuName = "ScriptableObjects/Chemistry/Reactions/VampireStageProgression")]
	public class VampireStageProgression : Chemistry.Effect
	{
		private int currentVampirismStage = 0;
		[SerializeField] private SicknessReaction vampirismReaction = null;
		[SerializeField] private Reagent vampirismReagent = null;
		[SerializeField] private Team vampireTeam = null;
		[SerializeField] private PreventCuredVampires preventCuredVampires = null;

		[SerializeField] private ItemTrait requiredTrait = null;

		[SerializeField] private List<StageAbilities> stageAbilities = new List<StageAbilities>();

		[System.Serializable]
		private class StageAbilities
		{
			public List<ActivatedAbility> ActivatedAbilities;
			public List<Mutation> Mutations;
		}

		[System.Serializable]
		private class ActivatedAbility
		{
			public ActionButtonData ButtonData;
			public Action<Vector2> ToTrigger;
		}

		public override void Apply(MonoBehaviour sender, ReagentMix reagentMix, Vector3 worldPosition, float amount)
		{
			if (sender == null) return;
			if (sender.TryGetComponent<ItemAttributesV2>(out var attributes) == false) return;
			if (requiredTrait == true && attributes.HasTrait(requiredTrait) == false) return;


			var metabolismComponent = sender as MetabolismComponent;
			if (metabolismComponent == false) return;

			float diseaseAmount = reagentMix[vampirismReagent];
			int vampireStage = vampirismReaction.GetStageIDFromReagentAmount(metabolismComponent, diseaseAmount);
			if(vampireStage == currentVampirismStage) return;
			if (vampireStage > currentVampirismStage) Evolve(metabolismComponent, vampireStage);
			else Devolve(metabolismComponent, vampireStage);
		}

		private void Devolve(MetabolismComponent organ, int newStage)
		{
			int stagesToGoThrough = currentVampirismStage - newStage;
			PlayerScript playerScript = organ.AssociatedSystem.Base.playerScript;
			if (playerScript.Mind.AntagPublic.CurTeam == vampireTeam)
			{
				preventCuredVampires.RemoveVampire(playerScript.Mind);
			}
			for (int i = currentVampirismStage; i >= newStage; i--)
			{
				StageAbilities abilitiesToGain = stageAbilities[i];
				foreach (var action in abilitiesToGain.ActivatedAbilities)
				{
					playerScript.Mind.PlayerButtonedActions.ServerRemoveAction(action.ButtonData.ID);
				}
				foreach (var mutation in abilitiesToGain.Mutations)
				{
					//Remove mutations
				}
			}

			currentVampirismStage = newStage;
		}

		private void Evolve(MetabolismComponent organ, int newStage)
		{
			int stagesToGoThrough = newStage - currentVampirismStage;

			PlayerScript playerScript = organ.AssociatedSystem.Base.playerScript;
			if (playerScript.Mind.AntagPublic.CurTeam != vampireTeam)
			{
				preventCuredVampires.AddNewVampire(playerScript.Mind);
			}

			for (int i = currentVampirismStage; i <= newStage; i++)
			{
				StageAbilities abilitiesToGain = stageAbilities[i];
				foreach (var action in abilitiesToGain.ActivatedAbilities)
				{
					playerScript.Mind.PlayerButtonedActions.ServerAddAction(action.ButtonData, action.ToTrigger);
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
