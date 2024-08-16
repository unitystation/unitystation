using System;
using Logs;
using Player.Movement;
using Systems.StatusesAndEffects.Interfaces;
using UnityEngine;

namespace Systems.StatusesAndEffects.Implementations
{
	[CreateAssetMenu(fileName = "Speed Buff", menuName = "ScriptableObjects/StatusEffects/SpeedBuff")]
	public class SpeedBuff : StatusEffect, IExpirableStatus, IMovementEffect, IStackableStatus
	{
		public event Action<IExpirableStatus> Expired;
		public float Duration => duration;
		public DateTime DeathTime { get; set; }
		public int InitialStacks { get; set; }
		public int Stacks { get; set; }
		public float RunningSpeedModifier => runningSpeedModifier;
		public float WalkingSpeedModifier => walkingSpeedModifier;
		public float CrawlingSpeedModifier => crawlingSpeedModifier;
		public float runningSpeedModifier = 12f;
		public float walkingSpeedModifier = 8f;
		public float crawlingSpeedModifier = 4f;
		public float duration = 30f;
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
			PlayerBase.playerMove.AddModifier(this);
			PlayerBase.BodyAlerts.RegisterAlert(SpeedBuffAlert);
		}

		public override void OnRemoved()
		{
			base.OnRemoved();
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CheckExpiration);
			if (Stacks <= 0)
			{
				PlayerBase?.playerMove.RemoveModifier(this);
				PlayerBase?.BodyAlerts.UnRegisterAlert(SpeedBuffAlert);
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