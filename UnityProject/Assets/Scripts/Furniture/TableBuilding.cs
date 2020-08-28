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

	[Tooltip("If apply Wood Plank.")]
	public LayerTile reinforcedTable;

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
			!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.WoodenPlank) &&
		!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.PlasteelSheet)){ return false; }
		if (interaction.HandObject.GetComponent<Stackable>().Amount < 2) return false;
		return true;
	}
	public void ServerPerformInteraction(HandApply interaction)
	{
		if (interaction.TargetObject != gameObject) return;
		if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench))
		{
			ToolUtils.ServerUseToolWithActionMessages(interaction, 0.5f,
				"You start deconstructing the table frame...",
				$"{interaction.Performer.ExpensiveName()} starts deconstructing the table frame...",
				"You finish deconstructing the table frame.",
				$"{interaction.Performer.ExpensiveName()} deconstructs the table frame.",
				() => Disassemble(interaction));
			SoundManager.PlayNetworkedAtPos("Wrench", gameObject.WorldPosServer(), 1f, sourceObj: gameObject);
		}
		else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.MetalSheet))
		{
			Assemble(interaction, "metal", metalTable, "Deconstruct");
		}
		else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.GlassSheet))
		{
			Assemble(interaction, "glass", glassTable, "GlassHit");
		}
		else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.WoodenPlank))
		{
			Assemble(interaction, "wooden", glassTable, "wood3");
		}
		else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.PlasteelSheet))
		{
			Assemble(interaction, "reinforced", glassTable, "Deconstruct");
		}
	}
	private void Assemble(HandApply interaction, string tableType, LayerTile layerTile, string soundName)
	{
		ToolUtils.ServerUseToolWithActionMessages(interaction, 0.5f,
			$"You start constructing a {tableType} table...",
			$"{interaction.Performer.ExpensiveName()} starts constructing a {tableType} table...",
			$"You finish assembling the {tableType} table.",
			$"{interaction.Performer.ExpensiveName()} assembles a {tableType} table.",
			() => SpawnTable(interaction, layerTile));
		SoundManager.PlayNetworkedAtPos(soundName, gameObject.WorldPosServer(), 1f, sourceObj: gameObject);
	}

	private void Disassemble(HandApply interaction)
	{
		Spawn.ServerPrefab("Rods", gameObject.WorldPosServer(), count: 2);
		Despawn.ServerSingle(gameObject);
	}

	private void SpawnTable(HandApply interaction, LayerTile tableToSpawn)
	{
		var interactableTiles = InteractableTiles.GetAt(interaction.TargetObject.TileWorldPosition(), true);
		Vector3Int cellPos = interactableTiles.WorldToCell(interaction.TargetObject.TileWorldPosition());
		interaction.HandObject.GetComponent<Stackable>().ServerConsume(2);
		interactableTiles.TileChangeManager.UpdateTile(cellPos, tableToSpawn);
		interactableTiles.TileChangeManager.SubsystemManager.UpdateAt(cellPos);
		Despawn.ServerSingle(gameObject);
	}

}