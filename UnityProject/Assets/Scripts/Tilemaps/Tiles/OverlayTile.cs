
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Tile which is merely an effect / overlay. Doesn't really
/// have any gameplay logic except for cleaning it up.
/// </summary>
[CreateAssetMenu(fileName = "Overlay", menuName = "Tiles/Overlay")]
public class OverlayTile : LayerTile
{
	[Tooltip("The unique name of this overlay, needed for gases as cant get name on main thread")]
	[SerializeField]
	private string overlayName = null;
	public string OverlayName => overlayName;

	[Tooltip("Appearance of this overlay")]
	[SerializeField]
	private Sprite sprite = null;
	public override Sprite PreviewSprite => sprite;

	[FormerlySerializedAs("isMoppable")]
	[Tooltip("Is this removed when the tile it's on is cleaned?")]
	[SerializeField]
	private bool isCleanable = false;
	public bool IsCleanable => isCleanable;

	[Tooltip("Is this tile graffiti")]
	[SerializeField]
	private bool isGraffiti = false;
	public bool IsGraffiti => isGraffiti;

	[Tooltip("The type of overlay?")]
	[SerializeField]
	private TileChangeManager.OverlayType overlayType = TileChangeManager.OverlayType.None;

	public TileChangeManager.OverlayType OverlayType => overlayType;
}
