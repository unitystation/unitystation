using System;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

public class Rack : NetworkBehaviour, ICheckedInteractable<PositionalHandApply>
{
	public GameObject rackParts;

	private Integrity integrity;

	private void Start()
	{
		integrity = gameObject.GetComponent<Integrity>();
		integrity.OnWillDestroyServer.AddListener(OnWillDestroyServer);
	}

	private void OnWillDestroyServer(DestructionInfo arg0)
	{
		Spawn.ServerPrefab(rackParts, gameObject.TileWorldPosition().To3Int(), transform.parent);
	}

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		if (!DefaultWillInteract.PositionalHandApply(interaction, NetworkSide.Client)) return false;

		return true;
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		PlayerNetworkActions pna = interaction.Performer.GetComponent<PlayerNetworkActions>();

		if (interaction.HandObject == null)
		{ // No item in hand, so let's TEACH THIS RACK A LESSON
			Chat.AddCombatMsgToChat(interaction.Performer, "You kick the rack. Nice job!",
				interaction.Performer.ExpensiveName() + " kicks the rack.");

			integrity.ApplyDamage(Random.Range(4, 8), AttackType.Melee, DamageType.Brute);
			return;
		}

		// If the player is using a wrench on the rack, deconstruct it
		if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench)
		    && interaction.Intent != Intent.Help)
		{
			SoundManager.PlayNetworkedAtPos("Wrench", interaction.WorldPositionTarget, 1f, sourceObj: interaction.Performer);
			Spawn.ServerPrefab(rackParts, interaction.WorldPositionTarget.RoundToInt(),
				interaction.TargetObject.transform.parent);
			Despawn.ServerSingle(gameObject);

			return;
		}

		//drop it right in the middle of the rack. IN order to do that we have to calculate
		//that position as an offset from the performer
		//TODO: Make it less awkward by adding a serverdrop method that accepts absolute position instead of vector.
		var targetTileWorldPosition = gameObject.TileWorldPosition();
		var targetTileVector =
			(Vector3Int) targetTileWorldPosition - interaction.PerformerPlayerScript.registerTile.WorldPositionServer;
		Inventory.ServerDrop(interaction.HandSlot, targetTileVector.To2Int());
	}
}
