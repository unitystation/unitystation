using UnityEngine;
using UnityEngine.Tilemaps;

namespace Tiles
{
	// TODO: obsolete?
	public class AnimatedTile : BasicTile
	{
		public Sprite[] Sprites;
		public float AnimationSpeed = 1f;
		public float AnimationStartTime = 0;
		public bool randomizeStartTime;

		public override Sprite PreviewSprite => Sprites.Length > 0 ? Sprites[0] : null;

		public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
		{
			tileAnimationData.animatedSprites = Sprites;
			tileAnimationData.animationSpeed = AnimationSpeed;
			if (!randomizeStartTime)
			{
				tileAnimationData.animationStartTime = AnimationStartTime;
			}
			else
			{
				tileAnimationData.animationStartTime = Random.Range(0f, 10f);
			}

			return true;
		}
	}
}
