using UnityEngine;
using UnityEngine.Tilemaps;


public class AnimatedTile : BasicTile
{
	public Sprite[] Sprites;
	public float AnimationSpeed = 1f;
	public float AnimationStartTime = 0;

	public override Sprite PreviewSprite => Sprites.Length > 0 ? Sprites[0] : null;

	public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
	{
		tileAnimationData.animatedSprites = Sprites;
		tileAnimationData.animationSpeed = AnimationSpeed;
		tileAnimationData.animationStartTime = AnimationStartTime;

		return true;
	}
}