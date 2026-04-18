using UnityEngine;

namespace US13.Systems.StatusesAndEffects
{
	public abstract class StatusEffect : ScriptableObject
	{
		public void Initialize(GameObject go)
		{
			OnAdded(go);
		}

		/// <summary>
		/// What should happen when this status is added to a manager.
		/// </summary>
		public virtual void OnAdded(GameObject target) {}

		/// <summary>
		/// What should ahppen when this status is removed from the manager
		/// </summary>
		public virtual void OnRemoved(GameObject target) {}

		/// <summary>
		/// What should happen when this status does its effect.
		/// </summary>
		public virtual void DoEffect(GameObject target) {}

		/// <summary>
		/// What should happen every update tick when this status does its effect?
		/// </summary>
		/// <param name="target"></param>
		public virtual void DoEffectTick(GameObject target) {}

		public override bool Equals(object other)
		{
			return other != null && other.GetHashCode() == GetHashCode();
		}

		public override int GetHashCode()
		{
			return name.GetHashCode();
		}
	}
}
