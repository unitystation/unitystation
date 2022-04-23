using UnityEngine;

namespace UI.Core.RightClick
{
	public interface IRadialPosition
	{
		bool IsWorldPosition { get; }
		Vector2 BoundsOffset { get; set; }
		Vector3 GetPositionIn(Camera camera, Canvas canvas);
	}
}
