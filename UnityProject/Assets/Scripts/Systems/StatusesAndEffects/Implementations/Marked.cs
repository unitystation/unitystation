using System;
using Systems.StatusesAndEffects.Interfaces;
using UnityEngine;

namespace Systems.StatusesAndEffects.Implementations
{
	[CreateAssetMenu(fileName = "Marked", menuName = "ScriptableObjects/StatusEffects/Marked")]
	public class Marked: StatusEffect, IExpirableStatus
	{
		public float duration = 30f;
		
		public event Action<IExpirableStatus> Expired;
		public float Duration => duration;
		public DateTime DeathTime { get; set; }
		
		public override void OnAdded()
		{
			DeathTime = DateTime.Now.AddSeconds(duration);
			UpdateManager.Add(CheckExpiration, 1f);
			//TODO: Marked sprite?
		}

		public override void OnRemoved()
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