using UnityEngine;

namespace US13.Systems.StatusesAndEffects.Interfaces
{
	public interface ICustomStatusEffectBehavior
	{
		/// <summary>
		/// What should happen when this status is added to a manager.
		/// </summary>
		public void ExtendedOnAdded(GameObject target);

		/// <summary>
		/// What should ahppen when this status is removed from the manager
		/// </summary>
		public void ExtendedOnRemoved(GameObject target);

		/// <summary>
		/// What should happen when this status does its effect.
		/// </summary>
		public void ExtendedDoEffect(GameObject target);

		/// <summary>
		/// What should happen every update tick when this status does its effect?
		/// </summary>
		/// <param name="target"></param>
		public void ExtendedDoEffectTick(GameObject target);
	}
}