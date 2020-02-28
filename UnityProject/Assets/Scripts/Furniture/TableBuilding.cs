using System;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

public class TableBuilding : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	[Tooltip("If apply Metal Sheet.")]
	public LayerTile metaltable;

	[Tooltip("If apply Glass Sheet.")]
	public LayerTile glassTable;

	[Tooltip("If apply Wood Plank.")]
	public LayerTile woodTable;

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
		Spawn.ServerPrefab("Rods", gameObject.TileWorldPosition().To3Int(), transform.parent,
			count: Random.Range(0, 3), scatterRadius: Random.Range(0f, 2f));
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		//start with the default HandApply WillInteract logic.
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		//only care about interactions targeting us
		if (interaction.TargetObject != gameObject) return false;
		//only try to interact if the user has a wrench, screwdriver in their hand
		if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench) &&
			!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.MetalSheet) &&
			!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.GlassSheet) &&
			!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.WoodenPlank)) { return false; }
		return true;
	}
	public void ServerPerformInteraction(HandApply interaction)
	{
		if (interaction.TargetObject != gameObject) return;
		if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench))
		{
			SoundManager.PlayNetworkedAtPos("Wrench", gameObject.WorldPosServer(), 1f);
			Disassemble(interaction);
			return;
		}
		else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.MetalSheet))
		{
			SoundManager.PlayNetworkedAtPos("Wrench", gameObject.WorldPosServer(), 1f);
			SpawnTable(interaction, metaltable);
			return;
		}
		else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.GlassSheet))
		{
			SoundManager.PlayNetworkedAtPos("Wrench", gameObject.WorldPosServer(), 1f);
			SpawnTable(interaction, glassTable);
			return;
		}
		else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.WoodenPlank))
		{
			SoundManager.PlayNetworkedAtPos("Wrench", gameObject.WorldPosServer(), 1f);
			SpawnTable(interaction, woodTable);
			return;
		}

	}
	[Server]
	private void Disassemble(HandApply interaction)
	{
		Spawn.ServerPrefab("Rods", gameObject.WorldPosServer(), count: howMany);
		Despawn.ServerSingle(gameObject);
	}
	[Server]
	private void SpawnTable(HandApply interaction, LayerTile tableToSpawn)
	{
		var interactableTiles = InteractableTiles.GetAt(interaction.TargetObject.TileWorldPosition(), true);
		Vector3Int cellPos = interactableTiles.WorldToCell(interaction.TargetObject.TileWorldPosition());
		interactableTiles.TileChangeManager.UpdateTile(cellPos, tableToSpawn);
		interactableTiles.TileChangeManager.SubsystemManager.UpdateAt(cellPos);
		Despawn.ServerSingle(gameObject);
	}

}

