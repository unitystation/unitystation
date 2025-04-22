using System.Collections.Generic;
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
		private float DiseaseMaxConcentrationPercent = 20f;

		public override void PossibleReaction(List<MetabolismComponent> senders, ReagentMix reagentMix,
		float reactionMultiple, float bodyReactionAmount, float TotalChemicalsProcessed, out bool overdose)
		{
			Debug.Log($"Reacting disease {name} starting with {reactionMultiple}u of disease in {reagentMix.Total}u of mix.");
			float diseaseAmount = reactionMultiple;

			MultiplyDisease(ref diseaseAmount, sicknessGrowthCharacteristic);
			Debug.Log($"Disease multiplied to {diseaseAmount}");

			float diseaseAmountPerOrgan = diseaseAmount / senders.Count;
			foreach (var metabolisedComponent in senders)
			{
				diseaseAmount -= ImmuneResponse(diseaseAmountPerOrgan, metabolisedComponent.componentImmuneResponse);
			}
			float concentrationPercent = (diseaseAmount / reagentMix.Total) * 100;
			if (concentrationPercent > DiseaseMaxConcentrationPercent)
				diseaseAmount *= (DiseaseMaxConcentrationPercent / concentrationPercent);

			SicknessStage currentStage = GetSicknessState(concentrationPercent);
			Debug.Log($"Disease Stage is {concentrationPercent}%");
			foreach (var stageEffect in currentStage.StageEffects)
			{
				foreach (var sender in senders)
				{
					stageEffect.Apply(sender, diseaseAmountPerOrgan);
				}
			}

			foreach (var ingredient in ingredients.m_dict)
			{
				reagentMix.Subtract(ingredient.Key, reactionMultiple * ingredient.Value);
			}

			foreach (var result in results.m_dict)
			{
				var reactionResult = diseaseAmount * result.Value;
				reagentMix.Add(result.Key, reactionResult);
			}

			overdose = false;
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
	}
}

