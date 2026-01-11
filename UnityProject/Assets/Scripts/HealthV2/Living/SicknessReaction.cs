using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Chemistry;
using HealthV2.Living.PolymorphicSystems.Bodypart;

namespace HealthV2.Sickness
{
	/// <summary>
	/// The characteristic immune response of a metabolising body part. Done on a part by part basis.
	/// </summary>
	[System.Serializable]
	public struct ImmuneResponse
	{
		/// <summary>
		/// The chance of an immune response occuring
		/// </summary>
		public float ImmuneResponseChancePercent;
		/// <summary>
		/// A flat static number that is always removed from the disease concentration on a successful response.
		/// </summary>
		public float ImmuneResponseBaseRate;
		/// <summary>
		/// The relative concentration of the disease that should be removed by this response. Float multiplier between 0 and 1 with 1 being a greater response.
		/// </summary>
		public float ImmuneResponseRelativeRate;
	}

	[System.Serializable]
	public struct SicknessGrowthCharacteristic
	{
		/// <summary>
		/// The chance of this sickness growing during an update
		/// </summary>
		public float SicknessGrowthChancePercent;
		/// <summary>
		/// A flat static number that is always added to the disease concentration on a successful update.
		/// </summary>
		public float SicknessGrowthBaseRate;
		/// <summary>
		/// The relative concentration of the disease that should be added by this growth. Float multiplier between 0 and 1 with 1 being a greater growth.
		/// </summary>
		public float SicknessGrowthRelativeRate;
	}

	[System.Serializable]
	public struct SicknessStage
	{
		public float StageConcentrationThreshold;
		public List<Chemistry.Effect> StageEffects;
	}

	/// <summary>
	/// Sickness reaction is designed around a chemical reaction with reactants 1u of disease and results 1u of disease.
	/// Effects of this disease at different stages is to be controlled by the 'stages' field on the SO. As well as its growth characteristics.
	/// Regular reaction effects WILL NOT be triggered by these reactions.
	/// </summary>
	[CreateAssetMenu(fileName = "NewSicknessReaction", menuName = "ScriptableObjects/Chemistry/SicknessReaction")]
	public class SicknessReaction : MetabolismReaction
	{
		[SerializeField] private SicknessGrowthCharacteristic sicknessGrowthCharacteristic = new SicknessGrowthCharacteristic();
		[SerializeField] private List<SicknessStage> stages = new List<SicknessStage>();

		[Tooltip("The largest %age of the bloodstream this disease can occupy. Caps its exponential growth."), SerializeField]
		private float DiseaseMaxConcentrationPercent = 16f;

		// We handle sickness reactions differently to standard metabolism reactions. Instead of isolating only 5u to react at a time,
		// We bypass this and react the whole mix. This is because sickness growth is not a result of a bodies metabolism but independent pathogen growth that scales universally.
		public override void PossibleReaction(List<MetabolismComponent> senders, ReagentMix reagentMix,
		float reactionMultiple, float bodyReactionAmount, float TotalChemicalsProcessed, float UntouchedMultiple, ref bool overdose)
		{
			if(senders.Count <= 0) return;

			//This line has the assumption that the only reagent for a sickness reaction is the pathogen.
			Reagent sicknessReagent = ingredients.m_dict.First().Key;

			float diseaseAmount = reagentMix[sicknessReagent];
			float initialAmount = diseaseAmount;

			MultiplyDisease(ref diseaseAmount, sicknessGrowthCharacteristic); //Calculate the natural growth of the disease

			float diseaseAmountPerOrgan = initialAmount / senders.Count;
			foreach (var metabolisedComponent in senders)
			{
				diseaseAmount -= ImmuneResponse(diseaseAmountPerOrgan, metabolisedComponent.componentImmuneResponse); //Apply an immune response on a per organ basis
			}

			int expectedBloodAmount = senders[0].AssociatedSystem.Base.reagentPoolSystem.NormalBlood;
			senders[0].AssociatedSystem.Base.HealthStateController.SetDirtyState(); //Tells the HuD it needs to update next time its update gets called.

			float concentrationPercent = (diseaseAmount / expectedBloodAmount) * 100;

			diseaseAmount = concentrationPercent > DiseaseMaxConcentrationPercent
				? diseaseAmount * (DiseaseMaxConcentrationPercent / concentrationPercent)
				: diseaseAmount; //Ensure disease does not exceed max concentration (Default 16%)

			SicknessStage currentStage = GetSicknessState(concentrationPercent); //Find the symptoms for the given concentration
			foreach (var stageEffect in currentStage.StageEffects) //Apply the symptoms
			{
				foreach (var sender in senders)
				{
					stageEffect.Apply(sender, reagentMix, sender.gameObject.AssumedWorldPosServer(), diseaseAmountPerOrgan);
				}
			}

			//Apply change in disease count.
			float change = diseaseAmount - initialAmount;
			if (change > 0)
			{
				foreach (var result in results.m_dict)
				{
					reagentMix.Add(result.Key, change * result.Value);
				}
				return;
			}

			foreach (var ingredient in ingredients.m_dict)
			{
				reagentMix.Remove(ingredient.Key, -(change * ingredient.Value));
			}
		}

		private static void MultiplyDisease(ref float diseaseAmount, SicknessGrowthCharacteristic sicknessGrowthCharacteristic)
		{
			if (DMMath.Prob(sicknessGrowthCharacteristic.SicknessGrowthChancePercent) == false) return;

			diseaseAmount += sicknessGrowthCharacteristic.SicknessGrowthBaseRate;
			diseaseAmount *= 1 + sicknessGrowthCharacteristic.SicknessGrowthRelativeRate;
		}

		private static float ImmuneResponse(float diseaseAmount, ImmuneResponse response)
		{
			if (DMMath.Prob(response.ImmuneResponseChancePercent) == false) return 0;

			float immuneResponseMagnitude = diseaseAmount * response.ImmuneResponseRelativeRate;
			return immuneResponseMagnitude + response.ImmuneResponseBaseRate;
		}

		private SicknessStage GetSicknessState(float concentrationPercent)
		{
			SicknessStage currentStage = stages[0];
			foreach (var stage in stages)
			{
				if (concentrationPercent > stage.StageConcentrationThreshold) currentStage = stage;
				else break;
			}

			return currentStage;
		}

		public int GetStageID(float concentrationPercent)
		{
			int currentStage = 0;
			for(int i = 0; i < stages.Count; i++)
			{
				if (concentrationPercent > stages[i].StageConcentrationThreshold) currentStage = i + 1;
				else break;
			}

			return currentStage;
		}
	}
}

