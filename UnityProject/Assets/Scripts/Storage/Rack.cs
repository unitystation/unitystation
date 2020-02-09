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
			SoundManager.PlayNetworkedAtPos("Wrench", interaction.WorldPositionTarget, 1f);
			Spawn.ServerPrefab(rackParts, interaction.WorldPositionTarget.RoundToInt(),
				interaction.TargetObject.transform.parent);
			Despawn.ServerSingle(gameObject);

			return;
		}

		// Like a table, but everything is stacked on center
		Vector3 targetPosition = interaction.WorldPositionTarget.RoundToInt();
		pna.CmdPlaceItem(interaction.HandSlot.NamedSlot.GetValueOrDefault(NamedSlot.none), targetPosition, interaction.Performer, true);
	}
}
