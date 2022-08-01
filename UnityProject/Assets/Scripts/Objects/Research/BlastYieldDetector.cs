using System;
using System.Collections.Generic;
using System.Text;
using Core.Editor.Attributes;
using Items.Weapons;
using Mirror;
using Systems.Electricity;
using UI.Core.Net;
using UnityEngine;

namespace Systems.Research.Objects
{
	public class BlastYieldDetector : ResearchPointMachine, ICanOpenNetTab, ICheckedInteractable<HandApply>
	{
		/// <summary>
		/// Distance the machine will detect blasts from.
		/// </summary>
		public float range;

		/// <summary>
		/// Direction the machine will detect blasts from.
		/// </summary>
		private Rotatable coneDirection;

		/// <summary>
		/// Randomized blast yield target for awarding maximum points, initialized from research server
		/// </summary>
		private int maxPointYieldTarget;

		/// <summary>
		/// Randomized blast yield target for awarding easier points, initialized from research server
		/// </summary>
		private int easyPointYieldTarget;

		/// <summary>
		/// Points awardable for reaching the more difficult blast yield target
		/// </summary>
		public int maxPointsValue;

		/// <summary>
		/// Points awardable for reaching the easier blast yield target
		/// </summary>
		public int easyPointsValue;

		/// <summary>
		/// Data structure for blast data, sorted so that scrolling through the graph makes sense
		/// </summary>
		public SortedList<float,float> blastData;

		protected RegisterObject registerObject;

		public delegate void BlastEvent();

		public delegate void ServerConnEvent(bool connected);

		public static event BlastEvent blastEvent;
		public static event ServerConnEvent serverConnEvent;

		[PrefabModeOnly]
		private SpriteHandler spriteHandler;

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

		private void UpdateGui()
		{
			// Change event runs UpdateNodes in GUI_BlastYieldDetector
			if (blastEvent != null)
			{
				blastEvent();
			}
		}

		private void Awake()
		{
			registerObject = GetComponent<RegisterObject>();
			coneDirection = GetComponent<Rotatable>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();

			ExplosiveBase.ExplosionEvent.AddListener(DetectBlast);
			blastData = new SortedList<float, float>();
			GetYieldTargets();
			AffirmState();
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

		/// <summary>
		/// Obtains Yield targets from Research.
		/// </summary>
		private void GetYieldTargets()
		{
			if (researchServer == null) return;
			researchServer.SetBlastYieldTargets();
			maxPointYieldTarget = researchServer.hardBlastYieldDetectorTarget;
			easyPointYieldTarget = researchServer.easyBlastYieldDetectorTarget;
			Chat.AddLocalMsgToChat("Yield targets acquired",gameObject);
		}

		/// <summary>
		/// Checks if an explosion happens within the detection cone. If the explosion takes
		/// place within the cone, the point value is checked and compared to previous alloted points.
		/// </summary>
		/// <param name="pos">Position of given explosion to check.</param>
		/// <param name="explosiveStrength">Blast yield of the given explosion.</param>
		private void DetectBlast(Vector3Int pos, float explosiveStrength)
		{
			Vector2 thisMachine = registerObject.WorldPosition.To2Int();

			float distance = Vector2.Distance(pos.To2Int(), thisMachine);
			//Distance is checked first to potentially avoid calculations.
			if (distance > range) return;

			//Math to check for if our explosion falls within a certain angle away from the center of the cone
			Vector2 coneToQuery = pos.To2Int() - thisMachine;
			coneToQuery.Normalize();

			Vector2 coneCenterVector = coneDirection.CurrentDirection.ToLocalVector2Int();
			coneCenterVector.Normalize();

			float angle = Math.Abs((Mathf.Acos(Vector2.Dot(coneToQuery, coneCenterVector)) * 180) / Mathf.PI);

			int points = calculateResearchPoints(explosiveStrength);

			if (angle <= 45)
			{
				blastData.Add(explosiveStrength, points);
				AwardResearchPoints(this, points);
			}

			UpdateGui();
		}

		/// <summary>
		/// Awards points by difference (this is handled by the ResearchServer).
		/// Total possible points are capped by the formula in calculateResearchPoints().
		/// </summary>
		/// <param name="source"></param>
		/// <param name="points"></param>
		public override int AwardResearchPoints(ResearchPointMachine source,int points)
		{
			int awarded = base.AwardResearchPoints(source, points);
			if (awarded> 0)
			{
				Chat.AddLocalMsgToChat($"Research points awarded: {awarded.ToString()}. New Total for Ordnance is {points.ToString()}", gameObject);
			}
			else
			{
				Chat.AddLocalMsgToChat("Explosion strength not close enough to yield " +
				                       "target to award additional research.",gameObject);
			}

			return awarded;
		}

		/// <summary>
		/// Determines the research point value of an explosion based on the following formula.
		/// [ max(e^(-(x - a1)^2/1250000)×140, e^(-(x - a2)^2/5000000)×70) ]
		/// a1 and a2 are the per round randomised values set between 1000 and 19000, modelled after minimum and maximum
		/// values for ExplosionBase ExplosionStrengths, that are targets to reach for blast yield.
		/// </summary>
		/// <param name="explosiveStrength"></param>
		/// <returns></returns>
		private int calculateResearchPoints(float explosiveStrength)
		{
			float term1 = (float)Math.Exp(-Math.Pow(explosiveStrength - maxPointYieldTarget, 2) / 1250000) *
			              maxPointsValue;
			float term2 = (float)Math.Exp(-Math.Pow(explosiveStrength - easyPointYieldTarget, 2) / 5000000) *
			              easyPointsValue;
			return (int)Mathf.Max(term1, term2);
		}

		#region Multitool Interaction Overrides

		public override void SubscribeToServerEvent(ResearchServer server)
		{
			base.SubscribeToServerEvent(server);
			ExplosiveBase.ExplosionEvent.AddListener(DetectBlast);
			Chat.AddLocalMsgToChat("Server connection found: Monitoring.",gameObject);
			stateSync = BlastYieldDetectorState.Connected;
			GetYieldTargets();
			serverConnEvent(true);
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

			blastData.Clear();
			easyPointYieldTarget = 0;
			maxPointYieldTarget = 0;
			serverConnEvent(false);
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
			if (stateSync == BlastYieldDetectorState.Connected)
			{
				if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench))
				{
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
			}
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