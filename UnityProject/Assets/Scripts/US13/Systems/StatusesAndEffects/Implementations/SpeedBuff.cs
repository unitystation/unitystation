using System;
using Logs;
using UnityEngine;
using US13.Items.Implants;
using US13.Managers.UpdateManager;
using US13.Player;
using US13.Systems.StatusesAndEffects.Interfaces;
using US13.UI.Core.Alerts;

namespace US13.Systems.StatusesAndEffects.Implementations
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

		public override void OnAdded(GameObject target)
		{
			DeathTime = DateTime.Now.AddSeconds(Duration);
			PlayerScript PlayerBase = target.GetComponent<PlayerScript>();
			UpdateManager.Add(CheckExpiration, 1f);
			if (PlayerBase == null)
			{
				Loggy.Warning($"Oi govna, can't make an inanimate object ({target}) belt it.");
				return;
			}
			foreach (var limb in PlayerBase.playerHealth.GetBodyFunctionsOfType<Limb>())
			{
				limb.SetNewEfficiency(Buff, this);
			}
			PlayerBase.BodyAlerts.RegisterAlert(SpeedBuffAlert);
		}

		public override void OnRemoved(GameObject target)
		{
			base.OnRemoved(target);
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CheckExpiration);
			if (target.TryGetComponent<PlayerScript>(out var playerBase) == false) return;
			playerBase.BodyAlerts.UnRegisterAlert(SpeedBuffAlert);
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