using System;
using System.Linq;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

public class InteractableFurniture : NetworkBehaviour, ICheckedInteractable<PositionalHandApply>
{
	[Tooltip("What it's made of.")]
	public GameObject resourcesMadeOf;

	[Tooltip("How many will it drop on deconstruct.")]
	public int howMany = 1;

	private Integrity integrity;

	//Testing Line Below
	public GameObject prefabVariant;

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

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		//start with the default HandApply WillInteract logic.
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (!Validations.HasComponent<InteractableTiles>(interaction.TargetObject)) return false;
		var vector = interaction.WorldPositionTarget.RoundToInt();
		if (!MatrixManager.IsPassableAt(vector, vector, false)) { return false; }
		return true;
		if (MatrixManager.GetAt<PlayerMove>(interaction.TargetObject, side)
			.Any(pm => pm.IsBuckled))
		{
			return false;
		}
		//only care about interactions targeting us
		if (interaction.TargetObject != gameObject) return false;
		//only try to interact if the user has a wrench, screwdriver in their hand
		if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench)) { return false; }
		return true;
	}

	[Server]
	private void Disassemble(PositionalHandApply interaction)
	{
		Spawn.ServerPrefab(resourcesMadeOf, gameObject.WorldPosServer(), count: howMany);
		Despawn.ServerSingle(gameObject);
	}
	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		// Spawn the opened body bag in the world
		Spawn.ServerPrefab(prefabVariant, interaction.WorldPositionTarget.RoundToInt(), interaction.Performer.transform.parent);

		// Remove the body bag from the player's inventory
		Inventory.ServerDespawn(interaction.HandSlot);
		if (interaction.TargetObject != gameObject) return;
		else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench))
		{
			ToolUtils.ServerPlayToolSound(interaction);
			Disassemble(interaction);
		}
	}
	
}

