
using Atmospherics;
using UnityEngine;

/// <summary>
/// Provides information on the fire an object is being exposed to.
/// If an object is atmos passable, an exposure can occur directly on it. However, if it
/// is not atmos passable, an exposure can occur from the side of the object - the fire brushes
/// against the object.
/// </summary>
public class FireExposure
{
	private readonly float temperature;
	private readonly Vector2Int hotspotLocalPosition;
	private readonly Vector2Int hotspotWorldPosition;
	private readonly Vector2Int atLocalPosition;
	private readonly Vector2Int atWorldPosition;

	/// <summary>
	/// True iff this is a side exposure (on a non-atmos-passable object)
	/// </summary>
	public bool IsSideExposure => atLocalPosition != hotspotLocalPosition;

	/// <summary>
	/// Temperature of the fire.
	/// </summary>
	public float Temperature => temperature;

	/// <summary>
	/// local tile position (within parent matrix) the hotspot is at.
	/// </summary>
	public Vector2Int HotspotLocalPosition => hotspotLocalPosition;

	/// <summary>
	/// world tile position (within parent matrix) the hotspot is at.
	/// </summary>
	public Vector2Int HotspotWorldPosition => hotspotWorldPosition;

	/// <summary>
	/// Position that is actually being exposed to this hotspot. This will not be
	/// the same as the HotspotLocalPosition if this is a side exposure.
	/// </summary>
	public Vector2Int ExposedLocalPosition => atLocalPosition;

	/// <summary>
	/// World position that is actually being exposed to this hotspot. This will not be
	/// the same as the HotspotWorldPosition if this is a side exposure.
	/// </summary>
	public Vector2Int ExposedWorldPosition => atWorldPosition;

	/// <summary>
	/// Returns the standard amount of damage done by this exposure. Not sure if
	/// this really will be a standard but using it for now to avoid code dup
	/// </summary>
	/// <returns></returns>
	public float StandardDamage()
	{
		return Mathf.Clamp(0.02f * Temperature, 0f, 20f);
	}

	private FireExposure(float temperature, Vector2Int hotspotLocalPosition, Vector2Int atLocalPosition, Vector2Int atWorldPosition, Vector2Int hotspotWorldPosition)
	{
		this.temperature = temperature;
		this.hotspotLocalPosition = hotspotLocalPosition;
		this.atLocalPosition = atLocalPosition;
		this.atWorldPosition = atWorldPosition;
		this.hotspotWorldPosition = hotspotWorldPosition;
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="hotspotNode">hotspot being exposed</param>
	/// <param name="hotspotWorldPosition">world position of the hotspot being exposed (so others don't need to recalculate it)</param>
	/// <param name="atLocalPosition">local position being exposed to</param>
	/// <param name="atWorldPosition">world position being exposed to (so others don't need to recalculate it)</param>
	/// <returns></returns>
	public static FireExposure FromMetaDataNode(MetaDataNode hotspotNode, Vector2Int hotspotWorldPosition,
		Vector2Int atLocalPosition, Vector2Int atWorldPosition)
	{
		if (!hotspotNode.HasHotspot)
		{
			Logger.LogErrorFormat("MetaDataNode at local position {0} has no hotspot, so no fire exposure" +
			                      " will occur. This is likely a coding error.", Category.Atmos, hotspotNode.Position);
			return null;
		}
		return new FireExposure(hotspotNode.Hotspot.Temperature, hotspotNode.Position.To2Int(), atLocalPosition, atWorldPosition, hotspotWorldPosition);
	}
}
