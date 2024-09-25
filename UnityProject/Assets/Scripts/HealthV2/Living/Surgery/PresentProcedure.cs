using System;
using Logs;

namespace HealthV2.Living.Surgery
{
	public class PresentProcedure
	{
		private readonly Random RNG = new();

		public Dissectible isOn;

		public SurgeryProcedureBase SurgeryProcedureBase;
		public int CurrentStep;
		public BodyPart RelatedBodyPart;

		//Used for when surgeries are cancelled
		public BodyPart PreviousBodyPart;
		public HandApply Stored;
		public SurgeryStep ThisSurgeryStep;

		public void TryTool(HandApply interaction)
		{
			if (interaction == null)
			{
				Loggy.LogError("[PresentProcedure] - Interaction is null!");
				return;
			}
			Stored = interaction;
			ThisSurgeryStep = null;
			isOn.NextTraitToUse = "";

			ThisSurgeryStep = SurgeryProcedureBase.SurgerySteps[CurrentStep];
			isOn.NextTraitToUse = ThisSurgeryStep?.RequiredTrait?.Name;

			if (ThisSurgeryStep != null)
			{
				if (Validations.HasItemTrait(interaction.HandObject, ThisSurgeryStep.RequiredTrait))
				{
					var StartSelf = ApplyChatModifiers(ThisSurgeryStep.StartSelf);
					var StartOther = ApplyChatModifiers(ThisSurgeryStep.StartOther);
					var SuccessSelf = ApplyChatModifiers(ThisSurgeryStep.SuccessSelf);
					var SuccessOther = ApplyChatModifiers(ThisSurgeryStep.SuccessOther);
					var FailSelf = ApplyChatModifiers(ThisSurgeryStep.FailSelf);
					var FailOther = ApplyChatModifiers(ThisSurgeryStep.FailOther);
					ToolUtils.ServerUseToolWithActionMessages(interaction.Performer, interaction.HandObject,
						ActionTarget.Object(interaction.TargetObject.RegisterTile()), ThisSurgeryStep.Time,
						StartSelf, StartOther,
						SuccessSelf, SuccessOther,
						SuccessfulProcedure,
						FailSelf, FailOther,
						UnsuccessfulProcedure);
					return;
				}
				PokeMessage(interaction);
			}
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Cautery))
			{
				Chat.AddActionMsgToChat(interaction,
					$" You use the {interaction.HandObject.ExpensiveName()} to close up {isOn.LivingHealthMasterBase.playerScript.visibleName}'s {RelatedBodyPart.name} ",
					$"{interaction.PerformerPlayerScript.visibleName} uses a {interaction.UsedObject.ExpensiveName()} to close up  {isOn.LivingHealthMasterBase.playerScript.visibleName}'s {RelatedBodyPart.name} ");
				CancelSurgery();
			}
		}

		private void PokeMessage(HandApply interaction)
		{
			if (interaction.UsedObject == null)
			{
				return;
			}
			//RelatedBodyPart can be null
			var pokelimbtext = RelatedBodyPart != null ? $"'s {RelatedBodyPart.name}" : "";
			Chat.AddActionMsgToChat(interaction,
				$" You poke {isOn.LivingHealthMasterBase.playerScript.visibleName}{pokelimbtext} with the {interaction.UsedObject.name} ",
				$"{interaction.PerformerPlayerScript.visibleName} pokes {isOn.LivingHealthMasterBase.playerScript.visibleName}{pokelimbtext} with the {interaction.UsedObject.name} ");
		}

		public void SuccessfulProcedure()
		{
			RelatedBodyPart?.HealthMaster.AddBleedStacks(ThisSurgeryStep.BleedStacksToAdd);
			if (RNG.Next(0, 100) < ThisSurgeryStep.FailChance)
			{
				UnsuccessfulProcedure();
				return;
			}
			CurrentStep++;
			if (CurrentStep >= SurgeryProcedureBase.SurgerySteps.Count)
			{
				isOn.ProcedureInProgress = false;

				SurgeryProcedureBase.FinnishSurgeryProcedure(RelatedBodyPart, Stored, this);

				RelatedBodyPart?.SuccessfulProcedure(Stored, this);
				isOn.ProcedureInProgress = false;
				//reset!
			}
			else
			{
				isOn.NextTraitToUse = ThisSurgeryStep?.RequiredTrait.Name;
			}
		}

		public void UnsuccessfulProcedure()
		{
			if (ThisSurgeryStep.BleedStacksToAdd > 0)
			{
				RelatedBodyPart?.HealthMaster.AddBleedStacks(ThisSurgeryStep.BleedStacksToAdd * 2);
			}
			SurgeryProcedureBase.UnsuccessfulStep(RelatedBodyPart, Stored, this);
			RelatedBodyPart?.UnsuccessfulStep(Stored, this);
		}

		public void CancelSurgery()
		{
			RelatedBodyPart = PreviousBodyPart;
			isOn.currentlyOn = RelatedBodyPart?.gameObject;
			CurrentStep = 0;
			SurgeryProcedureBase = null;
			isOn.ProcedureInProgress = false;
		}

		public void Clean()
		{
			PreviousBodyPart = RelatedBodyPart;
			RelatedBodyPart = null;
			CurrentStep = 0;
			SurgeryProcedureBase = null;
		}

		public void SetupProcedure(Dissectible dissectible, BodyPart bodyPart,
			SurgeryProcedureBase inSurgeryProcedureBase)
		{
			Clean();
			if (isOn != dissectible) PreviousBodyPart = null;

			isOn = dissectible;
			isOn.ProcedureInProgress = true;
			RelatedBodyPart = bodyPart;
			SurgeryProcedureBase = inSurgeryProcedureBase;
			ThisSurgeryStep = SurgeryProcedureBase?.SurgerySteps[CurrentStep];
			dissectible.NextTraitToUse = ThisSurgeryStep?.RequiredTrait.Name;
		}

		/// <summary>
		///     Replaces $ tags with their wanted names.
		/// </summary>
		/// <param name="toReplace">must have performer, Using and/or OnPart tags to replace.</param>
		/// <returns></returns>
		public string ApplyChatModifiers(string toReplace)
		{
			if (!string.IsNullOrWhiteSpace(toReplace))
				toReplace = toReplace.Replace("{WhoOn}", Stored.TargetObject.ExpensiveName());

			if (!string.IsNullOrWhiteSpace(toReplace))
				toReplace = toReplace.Replace("{performer}", Stored.Performer.ExpensiveName());

			if (!string.IsNullOrWhiteSpace(toReplace))
				toReplace = toReplace.Replace("{Using}", Stored.UsedObject.ExpensiveName());

			if (!string.IsNullOrWhiteSpace(toReplace))
				toReplace = toReplace.Replace("{OnPart}",
					RelatedBodyPart.OrNull()?.gameObject.OrNull()?.ExpensiveName());

			return toReplace;
		}
	}
}