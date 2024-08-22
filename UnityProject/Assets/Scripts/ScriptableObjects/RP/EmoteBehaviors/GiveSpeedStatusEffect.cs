using Systems.StatusesAndEffects;
using UnityEngine;

namespace ScriptableObjects.RP.EmoteBehaviors
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