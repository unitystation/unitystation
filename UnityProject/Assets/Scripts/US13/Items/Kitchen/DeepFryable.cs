using System;
using Chemistry;
using Mirror;
using UnityEngine;
using US13.ChemistryComponents;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Sprite_Handler;
using US13.Items.Food;
using Util;

namespace US13.Items.Kitchen
{
	public class DeepFryable: NetworkBehaviour, IExaminable
	{
		private const float FRYING_TIME_FRIED = 15f;
		private const float FRYING_TIME_DEEP_FRIED = 50f;
		private const float FRYING_TIME_THE_VERY_CONCEPT = 85f;

		private static readonly int fryIntensityId = Shader.PropertyToID("_FryIntensity");

		[SerializeField] private ItemAttributesV2 itemAttributes;
		[SerializeField] private SpriteHandler spriteHandler;
		[SerializeField] private Reagent friedNutrimentReagent;

		/// <summary>
		/// Name of the item when the current fry session started.
		/// Used to replace prefixes within a session without stacking.
		/// </summary>
		[SyncVar] private string nameAtSessionStart;

		[field: SyncVar]
		public FriedLevel Level { get; private set; }

		[field: SyncVar(hook = nameof(OnHighestLevelChanged))]
		public FriedLevel HighestLevel { get; private set; }

		private float secondsFrying;

		/// <summary>
		/// Adds frying time. Called by the deep fryer each tick.
		/// Returns true if the item just crossed the DeepFried (perfection) threshold.
		/// </summary>
		[Server]
		public bool AddFryingTime(float time)
		{
			if (Level >= FriedLevel.TheVeryConcept) return false;

			secondsFrying += time;
			FriedLevel newLevel = ComputeLevel(secondsFrying);

			if (newLevel == Level) return false;

			bool justReachedPerfection = newLevel == FriedLevel.DeepFried && Level < FriedLevel.DeepFried;
			Level = newLevel;
			if (newLevel > HighestLevel) HighestLevel = newLevel;
			ApplyName(newLevel);
			UpdateNutriment(newLevel);
			return justReachedPerfection;
		}

		/// <summary>
		/// Called when the item enters the fryer. Caches the current name and
		/// immediately sets the level to LightlyFried.
		/// </summary>
		[Server]
		public void StartFrySession()
		{
			nameAtSessionStart = itemAttributes.ArticleName;
			secondsFrying = 0f;

			if (Level >= FriedLevel.TheVeryConcept) return;
			Level = FriedLevel.LightlyFried;
			if (FriedLevel.LightlyFried > HighestLevel) HighestLevel = FriedLevel.LightlyFried;
			ApplyName(FriedLevel.LightlyFried);
			UpdateNutriment(FriedLevel.LightlyFried);
		}

		/// <summary>
		/// Called when the item leaves the fryer. Clears session state.
		/// </summary>
		[Server]
		public void EndFrySession()
		{
			nameAtSessionStart = null;
		}

		private static FriedLevel ComputeLevel(float seconds) =>
			seconds switch
			{
				>= FRYING_TIME_THE_VERY_CONCEPT => FriedLevel.TheVeryConcept,
				>= FRYING_TIME_DEEP_FRIED => FriedLevel.DeepFried,
				>= FRYING_TIME_FRIED => FriedLevel.Fried,
				_ => FriedLevel.LightlyFried
			};

		[Server]
		private void ApplyName(FriedLevel level)
		{
			string baseName = nameAtSessionStart ?? itemAttributes.ArticleName;
			string newName = level switch
			{
				FriedLevel.LightlyFried => $"lightly-fried {baseName}",
				FriedLevel.Fried => $"fried {baseName}",
				FriedLevel.DeepFried => $"deep-fried {baseName}",
				FriedLevel.TheVeryConcept => "the physical manifestation of the very concept of fried foods",
				_ => baseName,
			};
			itemAttributes.ServerSetArticleName(newName);
		}

		#region Dormant Component Activation

		private void ActivateDormantComponents()
		{
			// If this item already has an active Edible, don't touch it
			var existingEdible = this.GetCachedComponent<Edible>();
			if (existingEdible != null && existingEdible.enabled) return;

			// Enable dormant ReagentContainer first (Edible depends on it)
			var reagentContainer = this.GetCachedComponent<ReagentContainer>();
			if (reagentContainer != null && reagentContainer.enabled == false)
			{
				reagentContainer.enabled = true;
			}

			// Enable dormant Edible and configure it for fried food
			if (existingEdible != null && existingEdible.enabled == false)
			{
				existingEdible.enabled = true;
				int bites = Mathf.Max(1, (int)itemAttributes.Size);
				existingEdible.SetMaxBites(bites, resetCurrentBites: true);
			}
		}

		[Server]
		private void UpdateNutriment(FriedLevel newLevel)
		{
			var reagentContainer = this.GetCachedComponent<ReagentContainer>();
			if (reagentContainer == null) return;

			float nutriment = NutrientForLevel(newLevel);

			// Remove any existing fried nutriment, then add the new amount
			if (friedNutrimentReagent != null)
			{
				reagentContainer.CurrentReagentMix.Remove(friedNutrimentReagent, reagentContainer.CurrentReagentMix[friedNutrimentReagent]);
				if (nutriment > 0f)
				{
					reagentContainer.CurrentReagentMix.Add(friedNutrimentReagent, nutriment);
				}
			}
		}

		private static float NutrientForLevel(FriedLevel level)
		{
			return level switch
			{
				FriedLevel.LightlyFried => 2f,
				FriedLevel.Fried => 5f,
				FriedLevel.DeepFried => 10f,
				_ => 0f,
			};
		}

		#endregion

		#region Visuals

		private void OnHighestLevelChanged(FriedLevel oldLevel, FriedLevel newLevel)
		{
			// Try enabling the dormant components. This call is done in both server and client.
			if (oldLevel == FriedLevel.NotFried && newLevel > FriedLevel.NotFried)
			{
				ActivateDormantComponents();
			}

			if (spriteHandler == null) return;
			SetFryIntensity(newLevel);
		}

		/// <summary>
		/// Modifies the fried intensity on the sprite shader.
		/// </summary>
		/// <param name="level"></param>
		private void SetFryIntensity(FriedLevel level)
		{
			SpriteRenderer sr = spriteHandler.SpriteRenderer;
			var block = new MaterialPropertyBlock();
			sr.GetPropertyBlock(block);
			block.SetFloat(fryIntensityId, FriedLevelToIntensity(level));
			sr.SetPropertyBlock(block);
		}

		private static float FriedLevelToIntensity(FriedLevel level)
		{
			return level switch
			{
				FriedLevel.LightlyFried => 0.25f,
				FriedLevel.Fried => 0.5f,
				FriedLevel.DeepFried => 0.75f,
				FriedLevel.TheVeryConcept => 1.0f,
				_ => 0f,
			};
		}

		#endregion

		public string Examine(Vector3 worldPos = default) =>
			Level switch
			{
				FriedLevel.NotFried => "",
				FriedLevel.LightlyFried => "It's been lightly fried.",
				FriedLevel.Fried => "It's fried.",
				FriedLevel.DeepFried => "It's deep-fried to perfection.",
				FriedLevel.TheVeryConcept => "",
				_ => throw new ArgumentOutOfRangeException()
			};
	}

	public enum FriedLevel
	{
		NotFried,
		LightlyFried,
		Fried,
		DeepFried,
		TheVeryConcept
	}
}
