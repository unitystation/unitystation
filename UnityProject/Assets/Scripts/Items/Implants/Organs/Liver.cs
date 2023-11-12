using System;
using System.Collections.Generic;
using System.Text;
using Chemistry;
using Chemistry.Components;
using HealthV2;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using Logs;
using ScriptableObjects;
using UnityEngine;

namespace Items.Implants.Organs
{
	public class Liver : BodyPartFunctionality
	{
		/// <summary>
		/// ReagentContainer which the liver uses to hold reagents it will process. Reagents like alcohol will be broken down into their ethanol reagent via ReactionSet
		/// </summary>
		[SerializeField] public ReagentContainer processingContainer;

		/// <summary>
		/// Alcoholic reagents that the liver will process, override to define what the liver will accept to break down
		/// </summary>
		[Tooltip("Alcoholic reagents that the liver will process")]
		[SerializeField] private AlcoholicDrinksSOScript Alcohols;

		/// <summary>
		/// Reagent that 'alcohols' are assumed to be processed into
		/// </summary>
		[Tooltip("Reagent that 'alcohols' are assumed to be processed into")]
		[SerializeField] private Reagent ethanolReagent;

		/// <summary>
		/// Reagents toxic to the race this bodypart relates to.
		/// </summary>
		[Tooltip("Reagents toxic to the race this bodypart relates to.")]
		[SerializeField] private List<Reagent> Toxins;

		/// <summary>
		/// Amount of reagents liver will attempt to pull from the blood. Affected by BodyPart efficiency
		/// </summary>
		[SerializeField] private float processAmount = 2f;




		private StringBuilder debug;
		private List<Tuple<Reagent, float>> tempArray;



		public ReagentCirculatedComponent ReagentCirculatedComponent;

		public override void Awake()
		{
			base.Awake();
			ReagentCirculatedComponent = this.GetComponentCustom<ReagentCirculatedComponent>();
		}

		public override void SetUpSystems()
		{
			tempArray = new List<Tuple<Reagent, float>>();
		}


		public override void ImplantPeriodicUpdate()
		{
			//Liver has failed or just generally unable to process things, so don't let it.
			if (RelatedPart.TotalModified == 0) return;
			debug = new StringBuilder();

			BloodToLiver(ReagentCirculatedComponent.AssociatedSystem.BloodPool);
			Processing();
			ReturnReagentsToBlood();

			//Logger.Log(debug.ToString(), Category.Health);
		}

		private void BloodToLiver(ReagentMix blood)
		{
			float tickPullProcessingAmnt =  RelatedPart.TotalModified *  processAmount;
			float drawnAmount = 0;
			//debug.AppendLine("==== STAGE 1 || BLOOD TO PROCESSING ====");

			//figure out how much we are going to process or remove
			lock (blood.reagents)
			{
				foreach (var reagent in blood.reagents.m_dict)
				{
					bool alcohol = Alcohols.HashAlcoholicReagents.Contains(reagent.Key);
					bool toxic = Toxins.Contains(reagent.Key);
					if (alcohol || toxic)
					{
						float amount = Mathf.Min(tickPullProcessingAmnt, reagent.Value);
						amount = Mathf.Min(amount,
							(processingContainer.MaxCapacity - processingContainer.ReagentMixTotal) - drawnAmount);
						tempArray.Add(new Tuple<Reagent, float>(reagent.Key, amount));

						if (processingContainer.IsFull)
						{
							Loggy.LogTrace("Liver is full, please try again. or don't.", Category.Health);
							break;
						}

						drawnAmount += amount;
						tickPullProcessingAmnt -= amount;
						if (tickPullProcessingAmnt <= 0) break;
					}
				}
			}

			//debug.AppendLine($"Drawn from blood to liver: {drawnAmount}");

			//take what we are gonna process or remove, out of the blood
			foreach (Tuple<Reagent, float> reagent in tempArray)
			{
				//debug.AppendLine($"{reagent.Item2.ToString(CultureInfo.DefaultThreadCurrentCulture)} of {reagent.Item1}\n");
				processingContainer.CurrentReagentMix.Add(reagent.Item1, reagent.Item2);
				blood.Remove(reagent.Item1, reagent.Item2);
			}

			tempArray.Clear();
		}

		private void Processing()
		{
			//debug.AppendLine("==== STAGE 2 || REMOVAL FROM LIVER ====");

			float tickClearAmount = RelatedPart.TotalModified *  processAmount;
			processingContainer.ReagentsChanged(true);
			//calculate what's going to be removed, seeing as most processing will happen in the reactionset
			lock (processingContainer.CurrentReagentMix.reagents)
			{
				foreach (Reagent reagent in processingContainer.CurrentReagentMix.reagents.Keys)
				{
					//TODO: remove check for toxins when they are more integrated with reactions, with a metabolism rate, and liver damage
					if (Toxins.Contains(reagent))
					{
						float amount = Mathf.Min(tickClearAmount, processingContainer.CurrentReagentMix[reagent]);

						//setup to remove from liver
						tempArray.Add(new Tuple<Reagent, float>(reagent, amount));

						tickClearAmount -= amount;
						if (tickClearAmount <= 0) break;
					}
				}
			}

			//remove what's going to be removed
			foreach (Tuple<Reagent, float> reagent in tempArray)
			{
				//debug.AppendLine($"{reagent.Item2}cc of {reagent.Item1}\n");
				processingContainer.CurrentReagentMix.Remove(reagent.Item1, reagent.Item2);
			}

			tempArray.Clear();

		}

		private void ReturnReagentsToBlood()
		{
			//debug.AppendLine("==== STAGE 3 || RETURN FROM LIVER ====");

			lock (processingContainer.CurrentReagentMix.reagents)
			{
				foreach (Reagent reagent in processingContainer.CurrentReagentMix.reagents.Keys)
				{
					if (Toxins.Contains(reagent) || reagent == ethanolReagent)
					{
						tempArray.Add(new Tuple<Reagent, float>(reagent, processingContainer.CurrentReagentMix[reagent]));
					}
				}
			}

			foreach (Tuple<Reagent, float> reagent in tempArray)
			{
				//debug.AppendLine($"{reagent.Item2}cc of {reagent.Item1}\n");
				processingContainer.CurrentReagentMix.Remove(reagent.Item1, reagent.Item2);
				ReagentCirculatedComponent.AssociatedSystem.BloodPool.Add(reagent.Item1, reagent.Item2);
			}

			tempArray.Clear();
		}
	}
}
