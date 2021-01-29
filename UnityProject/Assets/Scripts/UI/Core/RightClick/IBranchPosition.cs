using UnityEngine;

namespace UI.Core.RightClick
{
	public interface IBranchPosition
	{
		bool IsWorldPosition { get; }
		Vector2 BoundsOffset { set; }
		Vector3 GetPositionIn(Camera camera, Canvas canvas);
	}
}
