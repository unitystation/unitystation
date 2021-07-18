using System;
using Systems.Explosions;
using UnityEngine;

namespace Items.Tool
{
	public class Igniter : MonoBehaviour, ICheckedInteractable<HandActivate>
	{
		private ObjectBehaviour objectBehaviour;

		private void Awake()
		{
			objectBehaviour = GetComponent<ObjectBehaviour>();
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
			SparkUtil.TrySpark(objectBehaviour, expose: false);

			var worldPos = objectBehaviour.AssumedWorldPositionServer();

			//Try start fire if possible
			var reactionManager = MatrixManager.AtPoint(worldPos, true).ReactionManager;
			reactionManager.ExposeHotspotWorldPosition(worldPos.To2Int(), 1000, true);
		}
	}
}
