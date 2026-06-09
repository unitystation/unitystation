using UnityEngine;
using US13.Core.Chat;
using US13.Core.Cooldowns;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Managers.MatrixManager;
using US13.Objects;
using US13.Systems.Explosions;
using UniversalObjectPhysics = US13.Core.Physics.UniversalObjectPhysics;

namespace US13.Items.Tool
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
