using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Rack : NBPositionalHandApplyInteractable
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
		PoolManager.PoolNetworkInstantiate(rackParts, gameObject.TileWorldPosition().To3Int(), transform.parent);
	}

	protected override bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (!base.WillInteract(interaction, side)) return false;

		if (!DefaultWillInteract.PositionalHandApply(interaction, NetworkSide.Client)) return false;

		return true;
	}

	protected override void ServerPerformInteraction(PositionalHandApply interaction)
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
		if (Validations.IsTool(interaction.HandObject, ToolType.Wrench)
		    && !interaction.Performer.Player().Script.playerMove.IsHelpIntent)
		{
			SoundManager.PlayNetworkedAtPos("Wrench", interaction.WorldPositionTarget, 1f);
			PoolManager.PoolNetworkInstantiate(rackParts, interaction.WorldPositionTarget.RoundToInt(),
				interaction.TargetObject.transform.parent);
			PoolManager.PoolNetworkDestroy(gameObject);

			return;
		}

		// Like a table, but everything is neatly stacked.
		Vector3 targetPosition = interaction.WorldPositionTarget.RoundToInt();
		targetPosition.z = -0.2f;
		pna.CmdPlaceItem(interaction.HandSlot.equipSlot, targetPosition, interaction.Performer, true);
	}
}
