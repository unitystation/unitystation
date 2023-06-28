using System;
using System.Collections.Generic;
using System.Text;
using Items.Weapons;
using Mirror;
using Systems.Electricity;
using UI.Core.Net;
using UnityEngine;
using Chemistry;

namespace Systems.Research.Objects
{
	public class BlastYieldDetector : ResearchPointMachine, ICanOpenNetTab, ICheckedInteractable<HandApply>
	{
		/// <summary>
		/// Distance the machine will detect blasts from.
		/// </summary>
		[SerializeField] private float range;

		/// <summary>
		/// Direction the machine will detect blasts from.
		/// </summary>
		private Rotatable coneDirection;

		/// <summary>
		/// A list of all the blasts detected, used to plot recent blast yields.
		/// </summary>
		public SyncList<float> BlastYieldData { get; private set; } = new SyncList<float>();

		protected RegisterObject registerObject;

		public delegate void BlastEvent(BlastData data);

		public delegate void UpdateGUIEvent();

		public static event BlastEvent blastEvent;
		public static event UpdateGUIEvent updateGUIEvent;

		[SerializeField]
		private SpriteHandler spriteHandler;

		[SerializeField]
		private List<Reaction> explosiveReactions = new List<Reaction>();

		public enum BlastYieldDetectorState
		{
			Off = 0,
			Connected = 1,
			Broken = 2
		}

		[SyncVar(hook = nameof(SyncSprite))]
		private BlastYieldDetectorState stateSync;

		private void SyncSprite(BlastYieldDetectorState oldState, BlastYieldDetectorState newState)
		{
			stateSync = newState;
			spriteHandler.ChangeSprite((int)newState);
		}

		private void Start()
		{
			registerObject = GetComponent<RegisterObject>();
			coneDirection = GetComponent<Rotatable>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();

			if (CustomNetworkManager.IsServer)
			{
				ExplosiveBase.ExplosionEvent.AddListener(DetectBlast);
				BlastYieldData.Clear();
			}

			AffirmState();
		}

		/// <summary>
		/// Checks if an explosion happens within the detection cone. If the explosion takes
		/// place within the cone, it attempts to complete bounties with the provided blastData
		/// </summary>
		/// <param name="pos">Position of given explosion to check.</param>
		/// <param name="blastData">The blast data from the explosion, contains yield and reagent mix if applicable.</param>
		private void DetectBlast(Vector3Int pos, BlastData blastData)
		{
			if (CustomNetworkManager.IsServer == false) return;

			Vector2 thisMachine = registerObject.WorldPosition.To2Int();

			float distance = Vector2.Distance(pos.To2Int(), thisMachine);
			//Distance is checked first to potentially avoid calculations.
			if (distance > range) return;

			//Math to check for if our explosion falls within a certain angle away from the center of the cone
			Vector2 coneToQuery = pos.To2Int() - thisMachine;
			coneToQuery.Normalize();

			Vector2 coneCenterVector = coneDirection.CurrentDirection.ToLocalVector2Int();
			coneCenterVector.Normalize();

			float angle = Vector2.Angle(coneCenterVector, coneToQuery);

			if (angle <= 45)
			{
				if (blastData.ReagentMix == null) blastData.ReagentMix = new ReagentMix();
				float yield = 0f;

				//This really isnt a nice way of doing it, but the way the chemistry assemblies are set up means I cannot just get the potency without cyclic references.
				//It's annoying but there is only a handful of explosive reactions anyways so its just better than nothing.
				//Ideally in the future someone reorganises the Chemistry assemblies but this works for now.
				foreach (Reaction reaction in explosiveReactions)
				{
					if (reaction.IsReactionValid(blastData.ReagentMix)) yield += ChemistryUtils.CalculateYieldFromReaction(reaction.GetReactionAmount(blastData.ReagentMix), 1);			
				}

				blastData.BlastYield += yield;

				BlastYieldData.Add(blastData.BlastYield);
				
				TryCompleteBounties(blastData);

				blastEvent?.Invoke(blastData);
			}
		}

		private void AffirmState()
		{
			if (PoweredState == PowerState.Off)
			{
				stateSync = BlastYieldDetectorState.Off;
			}
			else
			{
				if (researchServer == null)
				{
					stateSync = BlastYieldDetectorState.Broken;
				}
				else
				{
					stateSync = BlastYieldDetectorState.Connected;
				}
			}
		}

		#region BountyValidation

		private const float ALLOWED_ERROR_PERCENT = 0.05f; //The allowed error in blast yield to still achieve target.

		private void TryCompleteBounties(BlastData blastData)
		{
			List<ExplosiveBounty> bountyList = new List<ExplosiveBounty>();
			researchServer?.ExplosiveBounties.CopyTo(bountyList); //To prevent list being modified during iteration


			foreach (ExplosiveBounty bounty in bountyList)
			{
				var mix = blastData.ReagentMix;

				if (MeetsYieldTarget(bounty, blastData.BlastYield) == false || MeetsReagentTargets(bounty, mix) == false || MeetsReactionTargets(bounty, mix) == false) continue;

				researchServer?.CompleteBounty(bounty);
			}
		}

		private bool MeetsYieldTarget(ExplosiveBounty bounty, float yield)
		{
			if (bounty.RequiredYield.RequiredAmount <= 1) return yield <= ALLOWED_ERROR_PERCENT; //This is here to prevent / 0 errors and any errors that may arrive from very small divisions.

			float yieldDiff = Math.Abs(bounty.RequiredYield.RequiredAmount - yield);

			return yieldDiff / bounty.RequiredYield.RequiredAmount <= ALLOWED_ERROR_PERCENT;
		}

		private bool MeetsReagentTargets(ExplosiveBounty bounty, ReagentMix mix)
		{
			foreach (ReagentBountyEntry reagent in bounty.RequiredReagents)
			{
				mix.reagents.m_dict.TryGetValue(reagent.RequiredReagent, out float reagentAmount);
				if (reagentAmount != reagent.RequiredAmount)
				{
					return false;
				}
			}

			return true;
		}

		private bool MeetsReactionTargets(ExplosiveBounty bounty, ReagentMix mix)
		{
			foreach (ReactionBountyEntry reaction in bounty.RequiredReactions)
			{
				if (reaction.RequiredReaction.IsReactionValid(mix) == false || reaction.RequiredReaction.GetReactionAmount(mix) != reaction.RequiredAmount)
				{
					return false;
				}
			}
			return true;
		}

		#endregion

		#region Multitool Interaction Overrides

		public override void SubscribeToServerEvent(ResearchServer server)
		{
			base.SubscribeToServerEvent(server);
			ExplosiveBase.ExplosionEvent.AddListener(DetectBlast);
			Chat.AddLocalMsgToChat("Server connection found: Monitoring.",gameObject);
			stateSync = BlastYieldDetectorState.Connected;
			updateGUIEvent?.Invoke();
		}

		public override void UnSubscribeFromServerEvent()
		{
			base.UnSubscribeFromServerEvent();
			ExplosiveBase.ExplosionEvent.RemoveListener(DetectBlast);

			if (PoweredState != PowerState.Off)
			{
				Chat.AddLocalMsgToChat("Lost server connection.", gameObject);
				stateSync = BlastYieldDetectorState.Broken;
			}

			BlastYieldData.Clear();
			updateGUIEvent?.Invoke();
		}

		#endregion Multitool Interaction Overrides

		#region ICanOpenNetTab

		public bool CanOpenNetTab(GameObject playerObject, NetTabType netTabType)
		{
			if (PoweredState == PowerState.Off)
			{
				Chat.AddExamineMsgFromServer(playerObject, $"{gameObject.ExpensiveName()} is unpowered");
				return false;
			}
			updateGUIEvent?.Invoke();
			return true;
		}

		#endregion ICanOpenNetTab

		#region Interactions

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Wrench))
			{
				return interaction.Intent != Intent.Harm;
			}
			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (stateSync != BlastYieldDetectorState.Connected) return;
			if (!Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench)) return;

			ToolUtils.ServerUseToolWithActionMessages(interaction, 2,
				$"You start to rotate the array of the {gameObject.ExpensiveName()}...",
				$"{interaction.Performer.ExpensiveName()} starts to rotate the array of the {gameObject.ExpensiveName()}...",
				$"You rotated the array of the {gameObject.ExpensiveName()}.",
				$"{interaction.Performer.ExpensiveName()} rotates the array of the {gameObject.ExpensiveName()}.",
				() =>
				{
					coneDirection.RotateBy(1);
				},
				playSound:true);


		}

		public override string Examine(Vector3 worldPos = default)
		{
			StringBuilder examineMessage = new StringBuilder();
			examineMessage.Append(base.Examine(worldPos));
			examineMessage.Append("Array is point");

			switch (coneDirection.CurrentDirection)
			{
				case OrientationEnum.Default:
					examineMessage.Append("less. ");
					break;
				case OrientationEnum.Up_By0:
					examineMessage.Append("ed station north. ");
					break;
				case OrientationEnum.Right_By270:
					examineMessage.Append("ed station east. ");
					break;
				case OrientationEnum.Down_By180:
					examineMessage.Append("ed station south. ");
					break;
				case OrientationEnum.Left_By90:
					examineMessage.Append("ed station west. ");
					break;
			}

			return examineMessage.ToString();
		}
		#endregion Interactions

		#region IAPCPowerable

		public override void StateUpdate(PowerState state)
		{
			base.StateUpdate(state);
			if (PoweredState == PowerState.Off)
			{
				stateSync = BlastYieldDetectorState.Off;
			}
			else if(PoweredState == PowerState.On)
			{
				stateSync = BlastYieldDetectorState.Connected;
			}
		}
		#endregion IAPCPowerable

	}
}