using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "AnimatedOverlayTile", menuName = "Tiles/AnimatedOverlayTile")]
public class AnimatedOverlayTile : OverlayTile
{

	public override Sprite PreviewSprite => Sprites.Length > 0 ? Sprites[0] : null;
}