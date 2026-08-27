using UnityEngine;
using US13.Systems.StatusesAndEffects;
using US13.Systems.StatusesAndEffects.Interfaces;

namespace Tests.StatusAndEffectsFramework
{
	public class ImmediateStatusEffect : StatusEffect, IImmediateEffect
	{
		public bool DidEffect { get; private set; } = false;
		public int EffectCount { get; private set; }

		public override void DoEffect(GameObject target)
		{
			DidEffect = true;
			EffectCount++;
		}
	}
}
