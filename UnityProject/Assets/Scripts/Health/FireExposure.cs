
using Logs;
using UnityEngine;
using Systems.Atmospherics;

/// <summary>
/// Provides information on the fire an object is being exposed to.
/// If an object is atmos passable, an exposure can occur directly on it. However, if it
/// is not atmos passable, an exposure can occur from the side of the object - the fire brushes
/// against the object.
///
/// Note that it can have its state changed - this is only to allow re-use of a FireExposure to avoid
/// creating GC when doing a lot of fire exposures on a lot of tiles during a large fire.
/// </summary>
public class FireExposure
{
	private float temperature;
	private Vector3Int hotspotLocalPosition;
	private Vector3Int hotspotWorldPosition;
	private Vector3Int atLocalPosition;
	private Vector3Int atWorldPosition;

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
	public Vector3Int HotspotLocalPosition => hotspotLocalPosition;

	/// <summary>
	/// world tile position (within parent matrix) the hotspot is at.
	/// </summary>
	public Vector3Int HotspotWorldPosition => hotspotWorldPosition;

	/// <summary>
	/// Position that is actually being exposed to this hotspot. This will not be
	/// the same as the HotspotLocalPosition if this is a side exposure.
	/// </summary>
	public Vector3Int ExposedLocalPosition => atLocalPosition;

	/// <summary>
	/// World position that is actually being exposed to this hotspot. This will not be
	/// the same as the HotspotWorldPosition if this is a side exposure.
	/// </summary>
	public Vector3Int ExposedWorldPosition => atWorldPosition;

	/// <summary>
	/// Returns the standard amount of damage done by this exposure. Not sure if
	/// this really will be a standard but using it for now to avoid code dup
	/// </summary>
	/// <returns></returns>
	public float StandardDamage()
	{
		//Fire temp is minimum 373.15k, at that temp we do 3.7315 damage
		return  Mathf.Clamp(0.01f * Temperature, 0f, 20f);
	}

	/// <summary>
	/// To allow for re-use. Use Update to assign values to it.
	/// </summary>
	public FireExposure()
	{
	}

	private FireExposure(float temperature, Vector3Int hotspotLocalPosition, Vector3Int atLocalPosition, Vector3Int atWorldPosition, Vector3Int hotspotWorldPosition)
	{
		this.temperature = temperature;
		this.hotspotLocalPosition = hotspotLocalPosition;
		this.atLocalPosition = atLocalPosition;
		this.atWorldPosition = atWorldPosition;
		this.hotspotWorldPosition = hotspotWorldPosition;
	}

	private void Update(float temperature, Vector3Int hotspotLocalPosition, Vector3Int atLocalPosition, Vector3Int atWorldPosition, Vector3Int hotspotWorldPosition)
	{
		this.temperature = temperature;
		this.hotspotLocalPosition = hotspotLocalPosition;
		this.atLocalPosition = atLocalPosition;
		this.atWorldPosition = atWorldPosition;
		this.hotspotWorldPosition = hotspotWorldPosition;
	}

	/// <summary>
	/// Modify this exposure to be for a different node / tile
	/// </summary>
	/// <param name="hotspotNode"></param>
	/// <param name="hotspotWorldPosition"></param>
	/// <param name="atLocalPosition"></param>
	/// <param name="atWorldPosition"></param>
	public void Update(MetaDataNode hotspotNode, Vector3Int hotspotWorldPosition,
		Vector3Int atLocalPosition, Vector3Int atWorldPosition)
	{
		if (!hotspotNode.HasHotspot)
		{
			Loggy.LogErrorFormat("MetaDataNode at local position {0} has no hotspot, so no fire exposure" +
			                      " will occur. This is likely a coding error.", Category.Atmos, hotspotNode.LocalPosition);
			return;
		}
		Update(hotspotNode.GasMix.Temperature, hotspotNode.LocalPosition, atLocalPosition, atWorldPosition, hotspotWorldPosition);
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="hotspotNode">hotspot being exposed</param>
	/// <param name="hotspotWorldPosition">world position of the hotspot being exposed (so others don't need to recalculate it)</param>
	/// <param name="atLocalPosition">local position being exposed to</param>
	/// <param name="atWorldPosition">world position being exposed to (so others don't need to recalculate it)</param>
	/// <returns></returns>
	public static FireExposure FromMetaDataNode(MetaDataNode hotspotNode, Vector3Int hotspotWorldPosition,
		Vector3Int atLocalPosition, Vector3Int atWorldPosition)
	{
		if (!hotspotNode.HasHotspot)
		{
			Loggy.LogErrorFormat("MetaDataNode at local position {0} has no hotspot, so no fire exposure" +
			                      " will occur. This is likely a coding error.", Category.Atmos, hotspotNode.LocalPosition);
			return null;
		}
		return new FireExposure(hotspotNode.GasMix.Temperature, hotspotNode.LocalPosition, atLocalPosition, atWorldPosition, hotspotWorldPosition);
	}
}
