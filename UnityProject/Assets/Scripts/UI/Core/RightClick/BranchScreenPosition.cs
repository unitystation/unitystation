using UnityEngine;

namespace UI.Core.RightClick
{
	public class BranchScreenPosition : IBranchPosition
	{
		public bool IsWorldPosition => false;

		private Vector3 Position { get; set; }

		public Vector2 BoundsOffset { get; set; }

		public Vector3 GetPositionIn(Camera camera, Canvas canvas)
		{
			var rect = canvas.pixelRect;
			return Position.Clamp(rect.min + BoundsOffset, rect.max - BoundsOffset);
		}

		public BranchScreenPosition SetPosition(Vector3 position)
		{
			Position = position;
			return this;
		}
	}
}
