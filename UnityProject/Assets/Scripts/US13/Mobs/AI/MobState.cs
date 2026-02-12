using System.Collections.Generic;
using UnityEngine;
using US13.Managers.UpdateManager;

namespace US13.Mobs.AI
{
	public abstract class MobState : MonoBehaviour
	{
		[field: SerializeField] public List<MobState> Blacklist { get; private set; } = new List<MobState>();
		[field: SerializeField] public CallbackType UpdateType { get; private set; } = CallbackType.PERIODIC_UPDATE;
		[field: SerializeField] public float PeriodicUpdateInterval { get; set; } = 1.5f;

		public abstract void OnEnterState(MobAI master);
		public abstract void OnExitState(MobAI master);
		public abstract void OnUpdateTick(MobAI master);
		public abstract bool HasGoal(MobAI master);
	}
}
