using UnityEngine;

public class TileMouseDrop : Interaction
{
	private readonly Vector2Int targetCellPos;
	/// <summary>
	/// Targeted cell position (local) on the interactable tiles.
	/// </summary>
	public Vector3Int TargetCellPos => (Vector3Int)targetCellPos;

	private readonly InteractableTiles targetInteractableTiles;
	/// <summary>
	/// Targeted interactable tiles which contains the tile.
	/// </summary>
	public InteractableTiles TargetInteractableTiles => targetInteractableTiles;
	/// <summary>
	/// TileChangeManager for the tilemap containing the targeted tile.
	/// </summary>
	public TileChangeManager TileChangeManager => targetInteractableTiles.TileChangeManager;

	private BasicTile basicTile;
	/// <summary>
	/// BasicTile (tile configuration) of the tile being targeted.
	/// </summary>
	public BasicTile BasicTile => basicTile;

	private readonly Vector2 targetVector;

	/// <summary>
	/// Targeted world position deduced from target vector and performer position.
	/// </summary>
	public Vector2 WorldPositionTarget => (Vector2)Performer.transform.position + targetVector;

	/// <summary>
	/// Vector pointing from the performer to the targeted position. Set to Vector2.zero if aiming at self.
	/// </summary>
	public Vector2 TargetVector => targetVector;

	public TileMouseDrop(GameObject performer, GameObject usedObject, Intent intent, Vector2Int targetCellPos,
		InteractableTiles targetInteractableTiles, BasicTile basicTile, Vector2 targetVector) : base(performer, usedObject, intent)
	{
		this.targetCellPos = targetCellPos;
		this.targetInteractableTiles = targetInteractableTiles;
		this.basicTile = basicTile;
		this.targetVector = targetVector;
	}
}