using UnityEngine;

/// <summary>
/// Uses a RectTransform to define a region on a sprite, which can be checked to see if a position is inside the region.
/// Useful for allowing multiple codepaths depending on where you click on something.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SpriteClickRegion : MonoBehaviour
{
	private Transform spriteTransform;
	private Rect localRect;

	public Vector2 SpritePosWorld => spriteTransform.position;
	public Vector2 SpritePosLocal => spriteTransform.localPosition;

	private void Awake()
	{
		spriteTransform = transform;
		var rectTransform = GetComponent<RectTransform>();

		Vector3[] localCorners = new Vector3[4];
		rectTransform.GetLocalCorners(localCorners);

		localRect = new Rect(localCorners[0], rectTransform.rect.size);
	}

	/// <summary>
	/// Determines if the given position is inside the RectTransform associated with this component.
	/// </summary>
	/// <param name="worldPosition">The world position with which to check against.</param>
	/// <returns>true if the position is inside the region.</returns>
	public bool Contains(Vector2 worldPosition)
	{
		Vector2 localPosition = worldPosition - SpritePosWorld;

		return ContainsLocal(localPosition);
	}

	/// <summary>
	/// Determines if the given position is inside the RectTransform associated with this component.
	/// </summary>
	/// <param name="localPosition">The local position (relative to the sprite position) with which to check against.</param>
	/// <returns>true if the position is inside the region.</returns>
	public bool ContainsLocal(Vector2 localPosition)
	{
		return localRect.Contains(localPosition);
	}

	/// <summary>
	/// For development: draws a green border for the RectTransform associated with this component.
	/// Useful for determining that the rectangle is indeed where you want it to be.
	/// Only works in editor play mode, with Gizmos enabled.
	/// </summary>
	public void DebugDrawRectangle()
	{
		Vector3 topLeft = SpritePosWorld + new Vector2(localRect.xMin, localRect.yMin);
		Vector3 bottomLeft = SpritePosWorld + new Vector2(localRect.xMin, localRect.yMax);
		Vector3 topRight = SpritePosWorld + new Vector2(localRect.xMax, localRect.yMin);
		Vector3 bottomRight = SpritePosWorld + new Vector2(localRect.xMax, localRect.yMax);

		Debug.DrawLine(topLeft, topRight, Color.green, 10, false);
		Debug.DrawLine(bottomLeft, bottomRight, Color.green, 10, false);
		Debug.DrawLine(topLeft, bottomLeft, Color.green, 10, false);
		Debug.DrawLine(topRight, bottomRight, Color.green, 10, false);
	}
}
