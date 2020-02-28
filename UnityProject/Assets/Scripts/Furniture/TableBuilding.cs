using System;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

public class TableBuilding : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	[Tooltip("If apply Metal Sheet.")]
	public LayerTile metalTable;

	[Tooltip("If apply Glass Sheet.")]
	public LayerTile glassTable;

	[Tooltip("If apply Wood Plank.")]
	public LayerTile woodTable;

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
			ToolUtils.ServerUseToolWithActionMessages(interaction, 0.5f,
						"You start constructing a glass table...",
						$"{interaction.Performer.ExpensiveName()} starts constructing a glass table...",
						"You are constructing a glass table.",
						$"{interaction.Performer.ExpensiveName()} constructs a glass table.",
						() => Disassemble(interaction));
			SoundManager.PlayNetworkedAtPos("Wrench", gameObject.WorldPosServer(), 1f);
			
			return;
		}
		else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.MetalSheet))
		{
			ToolUtils.ServerUseToolWithActionMessages(interaction, 0.5f,
						"You start constructing a metal table...",
						$"{interaction.Performer.ExpensiveName()} starts constructing a metal table...",
						"You are constructing a metal table.",
						$"{interaction.Performer.ExpensiveName()} constructs a metal table.",
						() => SpawnTable(interaction, metalTable));
			;
			return;
		}
		else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.GlassSheet))
		{
			ToolUtils.ServerUseToolWithActionMessages(interaction, 0.5f,
						"You start constructing a glass table...",
						$"{interaction.Performer.ExpensiveName()} starts constructing a glass table...",
						"You are constructing a glass table.",
						$"{interaction.Performer.ExpensiveName()} constructs a glass table.",
						() => SpawnTable(interaction, glassTable));
			SoundManager.PlayNetworkedAtPos("GlassHit", gameObject.WorldPosServer(), 1f);
			
			return;
		}
		else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.WoodenPlank))
		{
			ToolUtils.ServerUseToolWithActionMessages(interaction, 0.5f,
						"You start constructing a wooden table...",
						$"{interaction.Performer.ExpensiveName()} starts constructing a wooden table...",
						"You are constructing a wooden table.",
						$"{interaction.Performer.ExpensiveName()} constructs a wooden table.",
						() => SpawnTable(interaction, woodTable));
			SoundManager.PlayNetworkedAtPos("wood3", gameObject.WorldPosServer(), 1f);
			
			return;
		}

	}
	[Server]
	private void Disassemble(HandApply interaction)
	{
		Spawn.ServerPrefab("Rods", gameObject.WorldPosServer(), count: 2);
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

