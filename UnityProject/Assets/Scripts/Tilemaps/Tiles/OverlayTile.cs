
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Tile which is merely an effect / overlay. Doesn't really
/// have any gameplay logic except for cleaning it up.
/// </summary>
[CreateAssetMenu(fileName = "Overlay", menuName = "Tiles/Overlay")]
public class OverlayTile : LayerTile
{

	[Tooltip("Appearance of this overlay")]
	[SerializeField]
	private Sprite sprite;
	public override Sprite PreviewSprite => sprite;

	[FormerlySerializedAs("isMoppable")]
	[Tooltip("Is this removed when the tile it's on is cleaned?")]
	[SerializeField]
	private bool isCleanable;
	public bool IsCleanable => isCleanable;
}
