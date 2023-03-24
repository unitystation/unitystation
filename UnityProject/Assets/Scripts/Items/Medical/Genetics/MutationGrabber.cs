using System.Collections;
using System.Collections.Generic;
using System.Text;
using HealthV2;
using Systems.MobAIs;
using UnityEngine;

public class MutationGrabber : MonoBehaviour, IExaminable , ICheckedInteractable<PositionalHandApply>, ICheckedInteractable<HandApply>
{

	public List<MutationSO> CarryingMutations = new List<MutationSO>();
	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		if (interaction.TargetObject == gameObject) return false;
		if (Validations.HasComponent<DinosaurLivingMutationCarrier>(interaction.TargetObject) == false && Validations.HasComponent<DNAConsole>(interaction.TargetObject) == false) return false;
		return true;
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		// can only be applied to LHB
		return Validations.HasComponent<LivingHealthMasterBase>(interaction.TargetObject);
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		var performerName = interaction.Performer.ExpensiveName();
		var targetName = interaction.TargetObject.ExpensiveName();
		Chat.AddActionMsgToChat(interaction.Performer,
			$"You analyze {targetName}'s DNA.",
			$"{performerName} analyzes {targetName}'s DNA.");
		var health = interaction.TargetObject.GetComponent<LivingHealthMasterBase>();
		StringBuilder scanMessage = new StringBuilder(
			"----------------------------------------\n" +
			$"{targetName} With stability of {health.Stability} \n");
		foreach (var bodypart in health.BodyPartList)
		{

			string toCustomise = "";
			string toadd = "";
			string toCapable = "";

			if (string.IsNullOrEmpty(bodypart.SetCustomisationData) == false)
			{
				toCustomise += "Customisation of " + bodypart.SetCustomisationData;
			}

			var mutations = bodypart.GetComponent<BodyPartMutations>();
			if (mutations != null)
			{
				if (mutations.ActiveMutations.Count > 0)
				{
					foreach (var mutation in mutations.ActiveMutations)
					{
						toadd += mutation.RelatedMutationSO.name + " " + mutation.Stability + ", ";
					}
				}

				if (interaction.IsAltClick)
				{
					if (mutations.CapableMutations.Count > 0)
					{
						foreach (var mutation in mutations.CapableMutations)
						{
							toCapable += mutation.name + " " + mutation.Stability + ", ";
						}
					}
				}
			}

			if (string.IsNullOrEmpty(toadd) == false)
			{
				scanMessage.AppendLine($"Body part : {bodypart.name} has " + toadd + " Mutations ");
			}

			if (string.IsNullOrEmpty(toCapable) == false)
			{
				scanMessage.AppendLine($"Body part : {bodypart.name} Is capable of  " + toCapable + " Mutations ");
			}
		}
		scanMessage.AppendLine($"----------- Customisation ------------------");
		foreach (var bodypart in health.BodyPartList)
		{
			string toCustomise = "";

			if (string.IsNullOrEmpty(bodypart.SetCustomisationData) == false)
			{
				toCustomise += "Customisation of " + bodypart.SetCustomisationData;
			}

			if (string.IsNullOrEmpty(toCustomise) == false)
			{
				scanMessage.AppendLine($"Body part : {bodypart.name} has Customisation of " + toCustomise );
			}
		}

		Chat.AddExamineMsgFromServer(interaction.Performer, $"</i>{scanMessage}<i>");
	}


	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		var  dinosaurLivingMutationCarrier = interaction.TargetObject.GetComponent<DinosaurLivingMutationCarrier>();

		if (dinosaurLivingMutationCarrier != null)
		{
			if (dinosaurLivingMutationCarrier.StageSynchronise == (dinosaurLivingMutationCarrier.GrowingStages.Count - 1))
			{
				foreach (var Mutation in dinosaurLivingMutationCarrier.CarryingMutations)
				{
					if (CarryingMutations.Contains(Mutation) == false)
					{
						CarryingMutations.Add(Mutation);
					}
				}
				Chat.AddExamineMsgFromServer(interaction.Performer, $" You scan and add the DNA mutations from {interaction.TargetObject.ExpensiveName()} to the buffer of {this.gameObject.ExpensiveName()} ");

				return;
			}
			else
			{
				string adding = "";
				var mobfood = dinosaurLivingMutationCarrier.GetComponent<MobExplore>();
				if (mobfood != null)
				{
					if (mobfood.HasFoodPrefereces)
					{
						adding = "Try feeding them some food Such as " ;
						foreach (var food in mobfood.FoodPreferences)
						{
							adding += food.name + ", ";
						}
					}
					else
					{
						adding = "Try feeding them some food";
					}

				}


				Chat.AddExamineMsgFromServer(interaction.Performer, $" The DNA mutations are too unstable from {interaction.TargetObject.ExpensiveName()} needs to become stabilised from growth. " + adding);

			}

		}
		var DNAConsole = interaction.TargetObject.GetComponent<DNAConsole>();
		if (DNAConsole != null)
		{
			List<MutationSO> mutations = new List<MutationSO>();
			foreach (var Mutation in CarryingMutations)
			{
				if (DNAConsole.UnlockedMutations.Contains(Mutation) == false)
				{
					mutations.Add(Mutation);

				}
			}

			foreach (var mutation in mutations)
			{
				DNAConsole.AddMutationOfficial(mutation);
			}

			Chat.AddExamineMsgFromServer(interaction.Performer, $" You plug-in the {this.gameObject.ExpensiveName()} Into the {interaction.TargetObject.ExpensiveName()} Transferring all the unknownMutations, And clearing the buffer ");
			CarryingMutations.Clear();
		}

	}

	string IExaminable.Examine(Vector3 worldPos)
	{
		if (CarryingMutations.Count == 0)
		{
			return "It's blooming empty";
		}
		else
		{
			var buildingstring = " Contained within the buffer is  ";
			foreach (var Mutation in CarryingMutations)
			{
				buildingstring += Mutation.name + ", ";
			}

			buildingstring += " and That is it ";
			return buildingstring;
		}
	}
}
