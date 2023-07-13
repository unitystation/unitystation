
using UnityEngine;

/// <summary>
/// Defines where an object should be spawned.
/// </summary>
public class SpawnDestination
{
	/// <summary>
	/// World position to spawn at. Defaults to HiddenPos.
	/// </summary>
	public Vector3 WorldPosition;

	/// <summary>
	/// Parent transform to spawn under. This does not usually need to be specified because the RegisterTile
	/// automatically figures out the correct parent.
	/// </summary>
	public readonly Transform Parent;

	/// <summary>
	/// Local rotation to spawn with. Defaults to Quaterion.identity (upright in parent matrix).
	/// </summary>
	public readonly Quaternion LocalRotation;

	/// <summary>
	/// if true, the spawn will be cancelled if the location being spawned into is totally impassable.
	/// </summary>
	public readonly bool CancelIfImpassable;

	/// <summary>
	/// if the object that commands the spawn is inside a storage (like a locker), spawn it inside it too
	/// </summary>
	public readonly UniversalObjectPhysics SharePosition;


	private SpawnDestination(Vector3 worldPosition, Transform parent, Quaternion localRotation,
		bool cancelIfImpassable, UniversalObjectPhysics sharePosition = null)
	{
		WorldPosition = worldPosition;
		Parent = parent;
		LocalRotation = localRotation;
		CancelIfImpassable = cancelIfImpassable;
		SharePosition = sharePosition;
	}

	/// <summary>
	/// Spawn destination at the indicated position.
	/// </summary>
	/// <param name="worldPosition">position to spawn at, defaults to HiddenPos</param>
	/// <param name="parent">Parent transform to spawn under. This does not usually need to be specified because the RegisterTile
	/// automatically figures out the correct parent.</param>
	/// <param name="rotation">rotation to spawn with ,defaults to Quaternion.identity</param>
	/// <param name="cancelIfImpassable">If true, the spawn will be cancelled if the location being spawned into is totally impassable.</param>
	/// <returns></returns>
	public static SpawnDestination At(Vector3? worldPosition = null, Transform parent = null,
		Quaternion? rotation = null, bool cancelIfImpassable = false, UniversalObjectPhysics sharePosition = null)
	{
		return new SpawnDestination(worldPosition.GetValueOrDefault(TransformState.HiddenPos),
			DefaultParent(parent, worldPosition), rotation.GetValueOrDefault(Quaternion.identity), cancelIfImpassable, sharePosition);
	}

	/// <summary>
	/// Creates a spawn destination at the existing object's position, with the same parent
	/// and rotation.
	/// </summary>
	/// <param name="existingObject">existing object to use to determine the destination</param>
	/// <param name="cancelIfImpassable">If true, the spawn will be cancelled if the location being spawned into is totally impassable.</param>
	/// <returns></returns>
	public static SpawnDestination At(GameObject existingObject, bool cancelIfImpassable = false)
	{
		var position = existingObject.AssumedWorldPosServer();
		var parent = existingObject.transform.parent;
		var localRotation = existingObject.transform.localRotation;
		return At(position, parent, localRotation, cancelIfImpassable);
	}

	/// <summary>
	/// Creates less garbage than using GameObject. Use this if you can get the register tile.
	///
	/// Creates a spawn destination at the existing object's position, with the same parent
	/// and rotation.
	/// </summary>
	/// <param name="existingObject">existing object to use to determine the destination</param>
	/// <param name="cancelIfImpassable">If true, the spawn will be cancelled if the location being spawned into is totally impassable.</param>
	/// <returns></returns>
	public static SpawnDestination At(RegisterTile existingRegisterTile, bool cancelIfImpassable = false)
	{
		var position = existingRegisterTile.WorldPositionServer;
		var parent = existingRegisterTile.transform.parent;
		var localRotation = existingRegisterTile.transform.localRotation;
		return At(position, parent, localRotation, cancelIfImpassable);
	}

	/// <summary>
	/// Spawn at hiddenpos, the place where invisible / not on stage stuff goes.
	/// </summary>
	/// <returns></returns>
	public static SpawnDestination HiddenPos()
	{
		return At();
	}

	/// <summary>
	/// Checks if the indicated tile is totally impassable.
	/// </summary>
	/// <param name="tileWorldPosition"></param>
	/// <returns></returns>
	public static bool IsTotallyImpassable(Vector3Int tileWorldPosition)
	{
		return!MatrixManager.IsPassableAtAllMatricesOneTile(tileWorldPosition,true)
		      &&!MatrixManager.IsAtmosPassableAt(tileWorldPosition,true);
	}

	private static Transform DefaultParent(Transform parent, Vector3? worldPos)
	{
		return parent != null ? parent : MatrixManager.GetDefaultParent(worldPos, true);
	}

	public override string ToString()
	{
		return $"{nameof(WorldPosition)}: {WorldPosition}, {nameof(Parent)}: {Parent}, {nameof(LocalRotation)}: " +
		       $"{LocalRotation}, {nameof(CancelIfImpassable)}: {CancelIfImpassable}";
	}
}