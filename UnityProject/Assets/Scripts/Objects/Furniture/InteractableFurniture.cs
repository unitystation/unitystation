using System;
using System.Linq;
using Mirror;
using Player.Movement;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Objects
{
	public class InteractableFurniture : NetworkBehaviour, ICheckedInteractable<HandApply>
	{
		[Tooltip("What it's made of.")]
		public GameObject resourcesMadeOf;

		[Tooltip("How many will it drop on deconstruct.")]
		public int howMany = 1;

		private Integrity integrity;

		private void Start()
		{
			integrity = gameObject.GetComponent<Integrity>();
			integrity.OnWillDestroyServer.AddListener(OnWillDestroyServer);
		}

		private void OnWillDestroyServer(DestructionInfo arg0)
		{
			Spawn.ServerPrefab(resourcesMadeOf, gameObject.TileWorldPosition().To3Int(), transform.parent,
				count: Random.Range(0, howMany + 1), scatterRadius: Random.Range(0f, 2f));
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			//start with the default HandApply WillInteract logic.
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			//only care about interactions targeting us
			if (interaction.TargetObject != gameObject) return false;
			//only try to interact if the user has a wrench, screwdriver in their hand
			if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench)) return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (MatrixManager.GetAt<MovementSynchronisation>(interaction.TargetObject, NetworkSide.Server)
					.Any(pm => pm.IsBuckled))
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"You cannot deconstruct this while it is occupied!");
				return;
			}

			ToolUtils.ServerPlayToolSound(interaction);
			Disassemble(interaction);
		}

		[Server]
		private void Disassemble(HandApply interaction)
		{
			Spawn.ServerPrefab(resourcesMadeOf, gameObject.AssumedWorldPosServer(), count: howMany);
			_ = Despawn.ServerSingle(gameObject);
		}
	}
}
