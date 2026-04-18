using System;
using UnityEngine;
using US13.Managers.UpdateManager;
using US13.Systems.StatusesAndEffects.Interfaces;

namespace US13.Systems.StatusesAndEffects.Implementations
{
	[CreateAssetMenu(fileName = "Marked", menuName = "ScriptableObjects/StatusEffects/Marked")]
	public class Marked: StatusEffect, IExpirableStatus
	{
		public float duration = 30f;

		public event Action<IExpirableStatus> Expired;
		public float Duration => duration;
		public DateTime DeathTime { get; set; }

		public override void OnAdded(GameObject target)
		{
			DeathTime = DateTime.Now.AddSeconds(duration);
			UpdateManager.Add(CheckExpiration, 1f);
			//TODO: Marked sprite?
		}

		public override void OnRemoved(GameObject target)
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CheckExpiration);
		}

		public void CheckExpiration()
		{
			if (DateTime.Now > DeathTime)
			{
				Expired?.Invoke(this);
			}
		}
	}
}