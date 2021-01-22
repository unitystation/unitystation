using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace HealthV2
{
	public class Dissectible : NetworkBehaviour, ICheckedInteractable<PositionalHandApply>
	{
		public LivingHealthMasterBase LivingHealthMasterBase;

		//needs set Surgery procedure

		public bool ProcedureInProgress = false;

		public BodyPart currentlyOn = null;

		public bool BodyPartIsopen = false;
		//sik var

		public PresentProcedure ThisPresentProcedure = new PresentProcedure();

		public List<ItemTrait> InitiateSurgeryItemTraits = new List<ItemTrait>(); //Make sure to include implantable stuff

		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (ProcedureInProgress == false)
			{
				if (side == NetworkSide.Client)
				{
					if (KeyboardInputManager.Instance.CheckKeyAction(KeyAction.InteractionModifier,
						KeyboardInputManager.KeyEventType.Hold) == false)
					{
						return false;
					}
				}

				if (Validations.HasAnyTrait(interaction.HandObject, InitiateSurgeryItemTraits))
				{
					return true;
				}
			}
			else
			{
				if (Validations.HasAnyTrait(interaction.HandObject, InitiateSurgeryItemTraits))
				{
					return true;
				}
			}

			return false;
		}


		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			if (ProcedureInProgress == false)
			{
				if (currentlyOn != null)
				{
					if (BodyPartIsopen)
					{
						var Options = currentlyOn.ContainBodyParts;


						UIManager.Instance.SurgeryDialogue.ShowDialogue(this , Options);
						//Show dialogue for  Pick organ and Procedure set it
					}
					else
					{
						var Options = currentlyOn;

						UIManager.Instance.SurgeryDialogue.ShowDialogue(this , Options);
						//Show dialogue for possible surgeries
					}
				}
				else
				{
					var Options = LivingHealthMasterBase.GetBodyPartsInZone(interaction.TargetBodyPart);
					UIManager.Instance.SurgeryDialogue.ShowDialogue(this , Options, true);
					//showDialogue box, For which body part
					// Set currently on and choose what surgery
				}
			}
			else
			{
				ThisPresentProcedure.TryTool(interaction);
				//Pass over to Body part being operated on
			}
		}

		public class PresentProcedure
		{
			System.Random RNG = new System.Random();

			public Dissectible ISon;

			public SurgeryProcedureBase SurgeryProcedureBase;
			public int CurrentStep = 0;
			public BodyPart RelatedBodyPart;

			public PositionalHandApply Stored;
			public SurgeryStep ThisSurgeryStep;


			public void TryTool(PositionalHandApply interaction)
			{
				Stored = interaction;
				ThisSurgeryStep = null;

				ThisSurgeryStep = SurgeryProcedureBase.SurgerySteps[CurrentStep];


				if (ThisSurgeryStep != null)
				{
					if (Validations.HasItemTrait(interaction.HandObject, ThisSurgeryStep.RequiredTrait))
					{
						ToolUtils.ServerUseToolWithActionMessages(interaction.Performer, interaction.HandObject,
							ActionTarget.Object(interaction.TargetObject.RegisterTile()), ThisSurgeryStep.Time,
							ThisSurgeryStep.StartSelf,
							ThisSurgeryStep.StartOther, ThisSurgeryStep.SuccessSelf, ThisSurgeryStep.SuccessOther,
							SuccessfulProcedure, ThisSurgeryStep.FailSelf, ThisSurgeryStep.FailOther, UnsuccessfulProcedure);
					}
				}
			}

			public void SuccessfulProcedure()
			{
				if (RNG.Next(0, 100) > ThisSurgeryStep.SuccessChance)
				{
					UnsuccessfulProcedure();
					return;
				}

				CurrentStep++;

				int NumberSteps = 0;
				NumberSteps = SurgeryProcedureBase.SurgerySteps.Count;
				if (CurrentStep >= NumberSteps)
				{
					ISon.ProcedureInProgress = false;

					SurgeryProcedureBase.FinnishSurgeryProcedure(RelatedBodyPart, Stored, this);

					RelatedBodyPart.SuccessfulProcedure(Stored, this);
					ISon.ProcedureInProgress = false;
					//reset!
				}
			}

			public void UnsuccessfulProcedure()
			{

				SurgeryProcedureBase.UnsuccessfulStep(RelatedBodyPart, Stored, this);

				RelatedBodyPart.UnsuccessfulStep(Stored, this);
			}

			public void Clean()
			{
				RelatedBodyPart = null;
				CurrentStep = 0;
				SurgeryProcedureBase = null;
			}

			public void SetupProcedure(Dissectible Dissectible, BodyPart  bodyPart, SurgeryProcedureBase inSurgeryProcedureBase)
			{
				Clean();
				ISon = Dissectible;
				ISon.ProcedureInProgress = true;
				RelatedBodyPart = bodyPart;
				SurgeryProcedureBase = inSurgeryProcedureBase;

			}
		}

		public enum ProcedureType
		{
			Close,
			Custom
		}
	}
}