using UnityEngine;
using US13.Player;
using US13.UI.Core.Alerts;
using Util;

namespace US13.Systems.StatusesAndEffects.Implementations.Hunger
{
	[CreateAssetMenu(fileName = "Hunger", menuName = "ScriptableObjects/StatusEffects/Hunger")]
	public class Vampirism : StatusEffect
	{
		public AlertSO StatusAlert;
		public string MessageOnEnteringStatus = "";
		public AlertUIElement

		public override void OnAdded()
		{
			base.OnAdded();
			var player = target.GetComponent<PlayerScript>();
			player.BodyAlerts.RegisterAlert(StatusAlert);

		}

		public override void OnRemoved()
		{
			base.OnRemoved();
			if (target != null)
			{
				var player = target.GetComponent<PlayerScript>();
				player.BodyAlerts.UnRegisterAlert(StatusAlert);
			}
		}

		public virtual void MessageWhenEnteringState()
		{
			if (MessageOnEnteringStatus == "") return;
			target.AddExamineMsgToChat(MessageOnEnteringStatus);
		}
	}
}