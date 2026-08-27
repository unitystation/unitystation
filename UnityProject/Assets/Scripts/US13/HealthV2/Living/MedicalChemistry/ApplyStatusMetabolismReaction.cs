using System.Collections.Generic;
using Chemistry;
using UnityEngine;
using US13.Core.Attributes;
using US13.HealthV2.Living.CirculatorySystem;
using US13.HealthV2.Living.PolymorphicSystems.Bodypart;
using US13.Systems.StatusesAndEffects;

namespace US13.HealthV2.Living.MedicalChemistry
{
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
