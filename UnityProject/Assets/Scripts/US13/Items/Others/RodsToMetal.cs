using Mirror;
using UnityEngine;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.ScriptableObjects;
using Util;

namespace US13.Items.Others
{
	public class RodsToMetal : NetworkBehaviour, ICheckedInteractable<HandApply>
	{
		[Tooltip("How many metal sheets the interaction should produce.")]
		[SerializeField]
		private int metalSpawnCount = 1;

		[Tooltip("How many rods needed for the interaction.")]
		[SerializeField]
		private int minimumRods = 2;

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			// Only active welder will transform rods.
			return Validations.HasUsedActiveWelder(interaction);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.TargetObject != gameObject) return;

			// Turn rods to metal
			ConvertRods(interaction);
		}

		[Server]
		private void ConvertRods(HandApply interaction)
		{
			Stackable stack = gameObject.GetComponent<Stackable>();
			if (stack.Amount >= minimumRods)
			{
				Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, interaction.Performer.AssumedWorldPosServer(), count: metalSpawnCount);
				stack.ServerConsume(minimumRods);
			}
		}
	}
}
