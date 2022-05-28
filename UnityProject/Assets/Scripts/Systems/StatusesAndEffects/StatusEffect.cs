using UnityEngine;

namespace Systems.StatusesAndEffects
{
	public abstract class StatusEffect: ScriptableObject
	{
		protected GameObject target;

		public void Initialize(GameObject go)
		{
			target = go;
			OnAdded();
		}

		/// <summary>
		/// What should happen when this status is added to a manager.
		/// </summary>
		public virtual void OnAdded() {}

		/// <summary>
		/// What should ahppen when this status is removed from the manager
		/// </summary>
		public virtual void OnRemoved() {}

		/// <summary>
		/// What should happen when this status does its effect.
		/// </summary>
		public virtual void DoEffect() {}

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
