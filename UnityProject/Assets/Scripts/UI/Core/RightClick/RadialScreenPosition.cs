using UnityEngine;

namespace UI.Core.RightClick
{
	public class RadialScreenPosition : IRadialPosition
	{
		public bool IsWorldPosition => false;

		private Vector3 Position { get; set; }

		public Vector2 BoundsOffset { get; set; }

		public bool IsRelativeToCenter { get; set; }

		public RadialScreenPosition(bool isRelativeToCenter = false)
		{
			IsRelativeToCenter = isRelativeToCenter;
		}

		public Vector3 GetPositionIn(Camera camera, Canvas canvas)
		{
			var rect = canvas.pixelRect;
			Vector3 position = IsRelativeToCenter ? Position + (Vector3)rect.size / 2 : Position;
			return position.Clamp(rect.min + BoundsOffset, rect.max - BoundsOffset);
		}

		public RadialScreenPosition SetPosition(Vector3 position)
		{
			Position = position;
			return this;
		}
	}
}
