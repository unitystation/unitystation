using System;
using HealthV2;
using Logs;
using Player.Movement;
using Systems.StatusesAndEffects.Interfaces;
using UnityEngine;

namespace Systems.StatusesAndEffects.Implementations
{
	[CreateAssetMenu(fileName = "Speed Buff", menuName = "ScriptableObjects/StatusEffects/SpeedBuff")]
	public class SpeedBuff : StatusEffect, IExpirableStatus
	{
		public event Action<IExpirableStatus> Expired;
		public float Duration => duration;
		public DateTime DeathTime { get; set; }
		public float duration = 30f;
		public float Buff = 1.25f;
		public AlertSO SpeedBuffAlert;

		private PlayerScript PlayerBase { get; set; }

		public override void OnAdded()
		{
			DeathTime = DateTime.Now.AddSeconds(Duration);
			PlayerBase = target.GetComponent<PlayerScript>();
			UpdateManager.Add(CheckExpiration, 1f);
			if (PlayerBase == null)
			{
				Loggy.LogWarning($"[SpeedBuff] - Oi govna, can't make an inanimate object ({target}) belt it.");
				return;
			}
			foreach (var limb in PlayerBase.playerHealth.GetBodyFunctionsOfType<Limb>())
			{
				limb.SetNewEfficiency(Buff, this);
			}
			PlayerBase.BodyAlerts.RegisterAlert(SpeedBuffAlert);
		}

		public override void OnRemoved()
		{
			base.OnRemoved();
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CheckExpiration);
			PlayerBase?.BodyAlerts.UnRegisterAlert(SpeedBuffAlert);
			if (PlayerBase == null) return;
			foreach (var limb in PlayerBase.playerHealth.GetBodyFunctionsOfType<Limb>())
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