using System.Collections.Generic;
using Logs;
using UnityEngine;
using US13.Core.Attributes;
using US13.Core.Lifecycle;
using US13.Core.Modular;
using US13.Items.Implants;
using US13.Player;
using US13.Systems.StatusesAndEffects.Interfaces;

namespace US13.Systems.StatusesAndEffects.Implementations.HorizontalStatusEffectBehaviors
{
	public class ControlLimbEfficiency : ICustomStatusEffectBehavior
	{
		public float LimbEfficiency = 1f;

		[field: SerializeReference, SelectImplementation(typeof(IConditional))]
		public List<IConditional> ExtensionConditions { get; set; } = new();

		public void ExtendedOnAdded(GameObject target)
		{
			PlayerScript playerBase = target.GetComponent<PlayerScript>();
			if (playerBase == null)
			{
				Loggy.Warning($"Oi govna, can't make an inanimate object ({target}) belt it.");
				return;
			}
			foreach (var limb in playerBase.playerHealth.GetBodyFunctionsOfType<Limb>())
			{
				limb.SetNewEfficiency(LimbEfficiency, this);
			}
		}

		public void ExtendedOnRemoved(GameObject target)
		{
			if (target.TryGetComponent<PlayerScript>(out var playerBase) == false) return;
			foreach (var limb in playerBase.playerHealth.GetBodyFunctionsOfType<Limb>())
			{
				limb.RemoveEfficiency(this);
			}
		}

		/// nothing needed to be done here
		public void ExtendedDoEffect(GameObject target)
		{
			return;
		}

		/// nothing needed to be done here
		public void ExtendedDoEffectTick(GameObject target)
		{
			return;
		}
	}
}