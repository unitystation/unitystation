
using UnityEngine;

/// <summary>
/// Encapsulates the information related to an attempt to start a progress action
/// </summary>
public class StartProgressInfo
{
	/// <summary>
	/// How many seconds it should take to complete.
	/// </summary>
	public readonly float TimeForCompletion;

	/// <summary>
	/// World position being targeted.
	/// </summary>
	public readonly Vector3 TargetWorldPosition;

	/// <summary>
	/// Local position being targeted, within the matrix being the targeted tile is on.
	/// </summary>
	public readonly Vector3Int TargetLocalPosition;

	/// <summary>
	/// Matrix info of the matrix containing the targeted tile.
	/// </summary>
	public readonly MatrixInfo TargetMatrixInfo;

	/// <summary>
	/// GameObject of the player performing the action.
	/// </summary>
	public readonly GameObject Performer;

	/// <summary>
	/// Progress bar representing this action.
	/// </summary>
	public readonly ProgressBar ProgressBar;

	public StartProgressInfo(float timeForCompletion, Vector3 targetWorldPosition, Vector3Int targetLocalPosition,
		MatrixInfo targetMatrixInfo, GameObject performer,
		ProgressBar progressBar)
	{
		TimeForCompletion = timeForCompletion;
		TargetWorldPosition = targetWorldPosition;
		TargetLocalPosition = targetLocalPosition;
		TargetMatrixInfo = targetMatrixInfo;
		Performer = performer;
		ProgressBar = progressBar;
	}
}
