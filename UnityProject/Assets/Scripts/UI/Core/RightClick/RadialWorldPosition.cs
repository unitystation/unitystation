using UnityEngine;

namespace UI.Core.RightClick
{
	public class RadialWorldPosition : IRadialPosition
	{
		public bool IsWorldPosition => true;

		private RegisterTile Tile { get; set; }

		public Vector2 BoundsOffset { get; set; }

		public Vector3 GetPositionIn(Camera camera, Canvas canvas)
		{
			var rect = canvas.pixelRect;

			if (Tile == null)
			{
				return rect.center;
			}

			var pos = camera.WorldToScreenPoint(Tile.WorldPosition);
			return pos.Clamp(rect.min - BoundsOffset, rect.max + BoundsOffset);
		}

		public RadialWorldPosition SetTile(RegisterTile tile)
		{
			Tile = tile;
			return this;
		}
	}
}
