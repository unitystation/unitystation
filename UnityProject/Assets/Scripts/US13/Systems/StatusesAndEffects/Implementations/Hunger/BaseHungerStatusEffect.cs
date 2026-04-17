using UnityEngine;
using US13.Player;
using US13.UI.Core.Alerts;
using Util;

namespace US13.Systems.StatusesAndEffects.Implementations.Hunger
{
	[CreateAssetMenu(fileName = "Hunger", menuName = "ScriptableObjects/StatusEffects/Hunger")]
	public class BaseHungerStatusEffect : StatusEffect
	{
		public AlertSO StatusAlert;
		public string MessageOnEnteringStatus = "";

		public override void OnAdded(GameObject target)
		{
			base.OnAdded(target);
			var player = target.GetComponent<PlayerScript>();
			player.BodyAlerts.RegisterAlert(StatusAlert);
			MessageWhenEnteringState(target);
		}

		public override void OnRemoved(GameObject target)
		{
			base.OnRemoved(target);
			if (target != null)
			{
				var player = target.GetComponent<PlayerScript>();
				player.BodyAlerts.UnRegisterAlert(StatusAlert);
			}
		}

		public virtual void MessageWhenEnteringState(GameObject target)
		{
			if (MessageOnEnteringStatus == "") return;
			target.AddExamineMsgToChat(MessageOnEnteringStatus);
		}
	}
}