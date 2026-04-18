using System;
using System.Collections.Generic;
using Chemistry;
using Mirror;
using NaughtyAttributes;
using SecureStuff;
using UnityEngine;
using US13.Actions.V2;
using US13.Core.Chat;
using US13.HealthV2.Living;
using US13.HealthV2.Living.MedicalChemistry;
using US13.HealthV2.Living.PolymorphicSystems;
using US13.Managers;
using US13.Player;
using US13.Systems.Antagonists.Objectives.TeamObjectives;
using Util;

namespace US13.Systems.Antagonists
{
	public partial class VampireStageProgression : NetworkBehaviour, ILightControl
	{
		[BoxGroup("Required References"), SerializeField] private SicknessReaction vampirismReaction = null;
		[BoxGroup("Required References"), SerializeField] private PreventCuredVampires preventCuredVampires = null;
		[BoxGroup("Required References"), SerializeField] private CorrectVampireAmount amountVampiresObjective = null;
		[BoxGroup("Required References"), SerializeField] private PlayerScript connectedPlayer;
		[BoxGroup("Required References"), SerializeField] private Antagonist vampireAntagonist = null;
		[BoxGroup("Required References"), SerializeField] private TeamData vampireTeam = null;



		[SerializeField] private List<StageAbilities> stageAbilities = new List<StageAbilities>();
		private int currentVampirismStage = -1;
		private int currentInGamePlayers = 0;

		private ReagentPoolSystem ReagentPool => connectedPlayer?.playerHealth?.reagentPoolSystem;

		[System.Serializable]
		private class StageAbilities
		{
			[field:SerializeField]
			public SerializableDictionary<ActionButtonData, SerializedAction> ActivatedAbilities { get; set; }
			public List<MutationSO> Mutations;
			public string onStageReachedText = "";
			public string onStageLostText = "";
		}


		public void Apply()
		{
			if (ReagentPool == null) return;

			TestForPlayerCountChange();
			float diseaseAmount = ReagentPool.BloodPool[CommonSicknesses.Instance.VampirismReagent];

			int vampireStage = vampirismReaction.GetStageIDFromReagentAmount(ReagentPool, diseaseAmount) - 1;
			if(vampireStage == currentVampirismStage) return;
			if (vampireStage > currentVampirismStage) Evolve(vampireStage);
			else Devolve(vampireStage);
		}

		private void TestForPlayerCountChange()
		{
			if (amountVampiresObjective.Team == null || currentInGamePlayers == PlayerList.Instance.InGamePlayers.Count) return;
			currentInGamePlayers = PlayerList.Instance.InGamePlayers.Count;
			amountVampiresObjective.UpdateObjectiveDescription();
		}

		private void Devolve(int newStage)
		{
			if (newStage <= 0 && connectedPlayer.Mind.AntagPublic != null && connectedPlayer.Mind.AntagPublic.CurTeam.Data == vampireTeam)
			{
				preventCuredVampires.RemoveVampire(connectedPlayer.Mind);
				connectedPlayer.Mind.RemoveAntag();
			}
			for (int i = Math.Max(currentVampirismStage,0); i >= newStage; i--)
			{
				StageAbilities abilitiesToGain = stageAbilities[i];
				foreach (var action in abilitiesToGain.ActivatedAbilities.Keys)
				{
					connectedPlayer.Mind.PlayerButtonedActions?.UnregisterAction(action);
				}

				if (abilitiesToGain.Mutations.Count == 0) continue;
				foreach (var bodyPart in connectedPlayer.playerHealth.BodyPartList)
				{
					if(bodyPart.CommonComponents.TryGetComponent<BodyPartMutations>(out BodyPartMutations bodyPartMutations) == false) continue;
					foreach (var mutation in abilitiesToGain.Mutations)
					{
						bodyPartMutations.RemoveMutation(mutation);
					}

				}

				Chat.AddWarningMsgFromServer(connectedPlayer.gameObject, abilitiesToGain.onStageLostText);

			}

			currentVampirismStage = newStage;
		}

		private void Evolve(int newStage)
		{
			TeamData currentTeam = connectedPlayer.Mind?.AntagPublic?.CurTeam?.Data;
			if (newStage > 0 && currentTeam != vampireTeam)
			{
				AntagManager.Instance.GetFirstTeamOrCreate(vampireTeam);
				preventCuredVampires.AddNewVampire(connectedPlayer.Mind);
				AntagManager.Instance.ServerFinishAntag(vampireAntagonist, connectedPlayer.Mind);
			}

			for (int i = Math.Max(currentVampirismStage,0); i <= newStage; i++)
			{
				StageAbilities abilitiesToGain = stageAbilities[i];
				foreach (var data in abilitiesToGain.ActivatedAbilities.Keys)
				{
					connectedPlayer.Mind.PlayerButtonedActions?.RegisterNewAction(data, abilitiesToGain.ActivatedAbilities[data].Invoke);
				}
				if (abilitiesToGain.Mutations.Count == 0) continue;
				foreach (var bodyPart in connectedPlayer.playerHealth.BodyPartList)
				{
					if(bodyPart.CommonComponents.TryGetComponent<BodyPartMutations>(out BodyPartMutations bodyPartMutations) == false) continue;
					foreach (var mutation in abilitiesToGain.Mutations)
					{
						bodyPartMutations.AddMutation(mutation);
					}

				}
				Chat.AddWarningMsgFromServer(connectedPlayer.gameObject, abilitiesToGain.onStageReachedText);
			}
			currentVampirismStage = newStage;
		}


	}
}
