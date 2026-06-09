using UnityEngine;
using US13.Systems.StatusesAndEffects;

namespace US13.ScriptableObjects.RP.EmoteBehaviors
{
	public class GiveSpeedStatusEffect : IEmoteBehavior
	{
		public StatusEffect Effect;
		public void Behave(GameObject actor)
		{
			actor.GetComponent<StatusEffectManager>()?.AddStatus(Effect);
		}
	}
}