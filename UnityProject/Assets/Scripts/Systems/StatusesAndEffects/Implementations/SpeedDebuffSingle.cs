using System;
using HealthV2;
using Logs;
using Systems.StatusesAndEffects.Interfaces;
using UnityEngine;

namespace Systems.StatusesAndEffects.Implementations
{
	[CreateAssetMenu(fileName = "Speed Debuff", menuName = "ScriptableObjects/StatusEffects/SpeedDebuff")]
	public class SpeedDebuffSingle : StatusEffect, IExpirableStatus
	{
		public event Action<IExpirableStatus> Expired;
		public float Duration => duration;
		public float duration = 2f;
		public DateTime DeathTime { get; set; }

		public float Debuff = 2.5f;


		public override void OnAdded()
		{
			DeathTime = DateTime.Now.AddSeconds(Duration);
			var playerBase = target.GetComponent<PlayerScript>();
			UpdateManager.Add(CheckExpiration, 1.5f);
			if (playerBase == null)
			{
				Loggy.Warning($"[SpeedBuff] - Oi govna, can't make an inanimate object ({target}) more inanimate.");
				return;
			}
			foreach (var limb in playerBase.playerHealth.GetBodyFunctionsOfType<Limb>())
			{
				limb.SetNewEfficiency(-Debuff, this);
			}
		}

		public override void OnRemoved()
		{
			base.OnRemoved();
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CheckExpiration);
			var playerBase = target.GetComponent<PlayerScript>();
			if (playerBase == null) return;
			foreach (var limb in playerBase.playerHealth.GetBodyFunctionsOfType<Limb>())
			{
				limb.RemoveEfficiency(this);
			}
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