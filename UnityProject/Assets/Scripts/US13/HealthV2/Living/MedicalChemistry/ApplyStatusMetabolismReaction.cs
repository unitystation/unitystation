using System;
using System.Collections.Generic;
using Chemistry;
using UnityEngine;
using US13.Core.Attributes;
using US13.HealthV2.Living.CirculatorySystem;
using US13.HealthV2.Living.PolymorphicSystems.Bodypart;
using US13.Systems.StatusesAndEffects;

namespace US13.HealthV2.Living.MedicalChemistry
{
	public interface IStatusMetabolismReagentSelector
	{
		bool HasMatch(ReagentMix reagentMix);
		void ConsumeMatches(ReagentMix reagentMix, float maxReactQuantity, float metabolismMultiplier);
	}

	[Serializable]
	public class AnyStatusMetabolismReagentSelector : IStatusMetabolismReagentSelector
	{
		public List<Reagent> Reagents = new();

		public bool HasMatch(ReagentMix reagentMix)
		{
			if (reagentMix == null) return false;
			foreach (var reagent in Reagents)
			{
				if (reagent != null && reagentMix[reagent] > 0) return true;
			}
			return false;
		}

		public void ConsumeMatches(ReagentMix reagentMix, float maxReactQuantity, float metabolismMultiplier)
		{
			if (reagentMix == null || reagentMix.Total == 0) return;
			var reactionScale = maxReactQuantity / reagentMix.Total;
			foreach (var reagent in Reagents)
			{
				if (reagent == null) continue;
				var untouchedMultiple = reagentMix[reagent];
				if (untouchedMultiple <= 0) continue;
				var reactionMultiple = Mathf.Min(untouchedMultiple * reactionScale * metabolismMultiplier, untouchedMultiple);
				reagentMix.Subtract(reagent, reactionMultiple);
			}
		}
	}

	[CreateAssetMenu(fileName = "ApplyStatusMetabolismReaction", menuName = "ScriptableObjects/Chemistry/Reactions/ApplyStatusMetabolismReaction")]
	public class ApplyStatusMetabolismReaction : MetabolismReaction
	{
		public StatusEffect StatusEffect;
		public StatusEffect SuppressedByStatusEffect;
		public StatusEffect StatusEffectToRemove;
		[SerializeReference, SelectImplementation(typeof(IStatusMetabolismReagentSelector))]
		public IStatusMetabolismReagentSelector ReagentSelector = new AnyStatusMetabolismReagentSelector();

		public override bool Apply(object sender, Vector3 WorldPosition, ReagentMix reagentMix)
		{
			if (ReagentSelector == null || ReagentSelector.HasMatch(reagentMix) == false) return false;

			var circulatorySystem = sender as IAreaReactionBase;
			if (circulatorySystem == null) return false;

			circulatorySystem.MetabolismReactions.Add(this);
			return false;
		}

		public override void React(List<MetabolismComponent> senders, ReagentMix reagentMix, float maxReactQuantity)
		{
			if (ReagentSelector == null || reagentMix == null || reagentMix.Total == 0) return;
			if (ReagentSelector.HasMatch(reagentMix) == false) return;

			ApplyStatus(senders);
			ReagentSelector.ConsumeMatches(reagentMix, maxReactQuantity, ReagentMetabolismMultiplier);
		}

		private void ApplyStatus(List<MetabolismComponent> senders)
		{
			if (StatusEffect == null || senders == null || senders.Count == 0) return;

			var health = senders[0]?.RelatedPart?.HealthMaster;
			if (health == null || health.gameObject == null) return;

			var statusEffectManager = health.gameObject.GetComponent<StatusEffectManager>();
			if (statusEffectManager == null) return;

			if (SuppressedByStatusEffect != null && statusEffectManager.HasStatus(SuppressedByStatusEffect)) return;

			if (StatusEffectToRemove != null && statusEffectManager.HasStatus(StatusEffectToRemove))
			{
				statusEffectManager.RemoveStatus(StatusEffectToRemove);
			}

			if (statusEffectManager.HasStatus(StatusEffect)) return;

			statusEffectManager.AddStatus(StatusEffect);
		}
	}
}
