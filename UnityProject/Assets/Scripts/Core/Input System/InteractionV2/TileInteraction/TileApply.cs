
using UnityEngine;

/// <summary>
/// Do not use this with interactable components.
/// Encapsulates the information for interacting with a tile on a particular matrix. Unlike other interactions, this is
/// not to be used for any interactable components and should instead only be used with TileInteraction SOs.
/// </summary>
public class TileApply : Interaction
{
	private readonly Vector2Int targetCellPos;
	/// <summary>
	/// Targeted cell position (local) on the interactable tiles.
	/// </summary>
	public Vector3Int TargetCellPos => (Vector3Int) targetCellPos;

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

	private readonly ItemSlot handSlot;
	public ItemSlot HandSlot => handSlot;

	/// <summary>
	/// Object being used in hand (same as UsedObject). Returns null if nothing in hand.
	/// </summary>
	public GameObject HandObject => UsedObject;

	private readonly Vector2 targetVector;

	/// <summary>
	/// Targeted world position deduced from target vector and performer position.
	/// </summary>
	public Vector2 WorldPositionTarget => (Vector2)Performer.transform.position + targetVector;

	/// <summary>
	/// Vector pointing from the performer to the targeted position. Set to Vector2.zero if aiming at self.
	/// </summary>
	public Vector2 TargetVector => targetVector;

	public enum ApplyType
	{
		HandApply,
		MouseDrop
	};

	private readonly ApplyType applyType;
	public ApplyType TileApplyType => applyType;

	/// <summary>
	///
	/// </summary>
	/// <param name="performer">performer of the interaction</param>
	/// <param name="usedObject">object in hand</param>
	/// <param name="intent">intent of the performer</param>
	/// <param name="targetCellPos">cell position being targeted on the interactable tiles</param>
	/// <param name="targetInteractableTiles">interactable tiles containing the tile being targeted</param>
	/// <param name="basicTile">info of the tile being targeted</param>
	/// <param name="handSlot">slot being used</param>
	/// <param name="targetVector">vector pointing from perform to the targeted position</param>
	public TileApply(GameObject performer, GameObject usedObject, Intent intent, Vector2Int targetCellPos,
		InteractableTiles targetInteractableTiles, BasicTile basicTile, ItemSlot handSlot, Vector2 targetVector, ApplyType type = ApplyType.HandApply) : base(performer, usedObject, intent)
	{
		this.targetCellPos = targetCellPos;
		this.targetInteractableTiles = targetInteractableTiles;
		this.basicTile = basicTile;
		this.handSlot = handSlot;
		this.targetVector = targetVector;
		this.applyType = type;
	}

	public override string ToString()
	{
		return $"{nameof(targetCellPos)}: {targetCellPos}, {nameof(targetInteractableTiles)}: {targetInteractableTiles}, {nameof(basicTile)}: {basicTile}, {nameof(handSlot)}: {handSlot}, {nameof(targetVector)}: {targetVector}, {nameof(TargetCellPos)}: {TargetCellPos}, {nameof(TargetInteractableTiles)}: " +
		       $"{TargetInteractableTiles}, {nameof(TileChangeManager)}: {TileChangeManager}, {nameof(BasicTile)}: " +
		       $"{BasicTile}, {nameof(HandSlot)}: {HandSlot}, {nameof(HandObject)}: {HandObject}, {nameof(WorldPositionTarget)}: " +
		       $"{WorldPositionTarget}, {nameof(TargetVector)}: {TargetVector}";
	}
}
