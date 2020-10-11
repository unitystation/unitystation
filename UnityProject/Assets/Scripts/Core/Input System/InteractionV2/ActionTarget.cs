
using UnityEngine;

/// <summary>
/// General purpose way of defining the target of an action, so that a tile or
/// a particular object can be targeted but use the same methods.
/// </summary>
public class ActionTarget
{
	/// <summary>
	/// Targeting a tile at a particular local position.
	/// </summary>
	public readonly bool IsTile;

	/// <summary>
	/// Targeting an object.
	/// </summary>
	public bool IsObject => !IsTile;

	/// <summary>
	/// RegisterTile of the targeted object, null if IsTile.
	/// </summary>
	public readonly RegisterTile Target;

	/// <summary>
	/// Targeted gameobject, null if IsTile.
	/// </summary>
	public GameObject TargetObject => IsTile ? null : Target.gameObject;

	/// <summary>
	/// Current world position of the tile or object being targeted. This will stay correct even
	/// if the target tile / object is on a moving matrix and the matrix is moving.
	/// </summary>
	public Vector3 TargetWorldPosition => TargetMatrixInfo.Objects.TransformPoint(TargetLocalPosition);

	/// <summary>
	/// Local position of the tile or object being targeted, within the matrix being the targeted tile is on.
	/// </summary>
	public readonly Vector3Int TargetLocalPosition;

	/// <summary>
	/// Matrix info of the matrix containing the targeted tile or object.
	/// </summary>
	public readonly MatrixInfo TargetMatrixInfo;

	private ActionTarget(bool isTile, RegisterTile target, Vector3Int targetLocalPosition, MatrixInfo targetMatrixInfo)
	{
		IsTile = isTile;
		Target = target;
		this.TargetLocalPosition = targetLocalPosition;
		TargetMatrixInfo = targetMatrixInfo;
	}

	/// <summary>
	/// Target the tile currently at the indicated world position.
	/// </summary>
	/// <param name="worldTilePos"></param>
	/// <returns></returns>
	public static ActionTarget Tile(Vector3 worldTilePos)
	{

		var targetTilePosition = worldTilePos.CutToInt();
		var targetMatrixInfo = MatrixManager.AtPoint(targetTilePosition, true);
		var targetParent = targetMatrixInfo.Objects;
		//snap to local position
		var targetLocalPosition = targetParent.transform.InverseTransformPoint(worldTilePos).RoundToInt();

		return new ActionTarget(true, null, targetLocalPosition, targetMatrixInfo);
	}

	/// <summary>
	/// Target the specified gameobject
	/// </summary>
	/// <param name="target"></param>
	/// <returns></returns>
	public static ActionTarget Object(RegisterTile target)
	{
		if (target == null) return null;

		return new ActionTarget(false, target, target.LocalPosition, MatrixManager.Get(target.Matrix.Id));
	}

	public override string ToString()
	{
		return $"{nameof(IsTile)}: {IsTile}, {nameof(Target)}: {Target}, {nameof(TargetLocalPosition)}: {TargetLocalPosition}, {nameof(TargetMatrixInfo)}: {TargetMatrixInfo}";
	}
}
