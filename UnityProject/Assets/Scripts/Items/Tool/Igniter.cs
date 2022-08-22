using System;
using Systems.Explosions;
using Objects;
using UnityEngine;

namespace Items.Tool
{
	public class Igniter : MonoBehaviour, ICheckedInteractable<HandActivate>, ITrapComponent
	{
		private UniversalObjectPhysics objectBehaviour;

		private void Awake()
		{
			objectBehaviour = GetComponent<UniversalObjectPhysics>();
		}

		private void Ignite()
		{
			SparkUtil.TrySpark(gameObject, expose: false);

			var worldPos = objectBehaviour.registerTile.WorldPosition;

			//Try start fire if possible
			var reactionManager = MatrixManager.AtPoint(worldPos, true).ReactionManager;
			reactionManager.ExposeHotspotWorldPosition(worldPos.To2Int(), 1000, true);
		}

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (Cooldowns.TryStart(interaction, this, 5f, side) == false)
			{
				Chat.AddExamineMsg(interaction.Performer, "The capacitors inside the igniter are still recharging", side);

				return false;
			}

			return true;
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			Ignite();
		}

		public void TriggerTrap()
		{
			Ignite();
		}
	}
}
