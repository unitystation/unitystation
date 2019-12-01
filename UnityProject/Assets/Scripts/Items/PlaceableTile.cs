using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Allows an item to be placed in order to create a tile on the ground.
/// </summary>
public class PlaceableTile : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
{
	[Tooltip("Layer tile which this will create when placed.")]
	[SerializeField]
	private LayerTile layerTile;
	/// <summary>
	/// Layer tile which this will create when placed.
	/// </summary>
	public LayerTile LayerTile => layerTile;

	[Tooltip("What layer this can be placed on top of. Choose None to allow placing on empty space.")]
	[SerializeField]
	private LayerType placeableOn;


	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.HandObject != gameObject) return false;

		//check if we are clicking a spot we can place a tile on
		var interactableTiles = InteractableTiles.GetAt(interaction.WorldPositionTarget, side);
		var tileAtPosition = interactableTiles.LayerTileAt(interaction.WorldPositionTarget);
		//open space
		if (tileAtPosition == null) return placeableOn == LayerType.None;
		// placing on an existing tile
		else return tileAtPosition.LayerType == placeableOn;
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		//which matrix are we clicking on
		var interactableTiles = InteractableTiles.GetAt(interaction.WorldPositionTarget, true);
		Vector3Int cellPos = interactableTiles.WorldToCell(interaction.WorldPositionTarget);
		interactableTiles.TileChangeManager.UpdateTile(cellPos, layerTile);

		Inventory.ServerConsume(interaction.HandSlot, 1);
	}
}
