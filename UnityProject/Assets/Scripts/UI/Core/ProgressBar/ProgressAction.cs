
using UnityEngine;

/// <summary>
/// Main API / entry point for working with progress actions.
/// </summary>
public static class ProgressAction
{
	/// <summary>
	/// Start a progress action targeting something.
	/// </summary>
	/// <param name="progressAction">progress action being performed</param>
	/// <param name="actionTarget">target of the action</param>
	/// <param name="timeForCompletion">how long in seconds the action should take</param>
	/// <param name="player">player performing the action</param>
	/// <returns>progress bar associated with this action (can use this to interrupt progress). Null if
	/// progress was not started for some reason (such as already in progress for this action on the specified tile).</returns>
	public static ProgressBar ServerStartProgress(this IProgressAction progressAction, ActionTarget actionTarget,
		float timeForCompletion,
		GameObject player)
	{
		return UIManager._ServerStartProgress(progressAction, actionTarget, timeForCompletion, player);
	}

	/// <summary>
	/// Start a progress action targeting a specific tile.
	/// Tries to create and begin animating a progress bar for a specific player. Returns null
	/// if progress did not begin for some reason.
	/// </summary>
	/// <param name="progressAction">progress action being performed</param>
	/// <param name="worldTilePos">tile position the action is being performed on</param>
	/// <param name="timeForCompletion">how long in seconds the action should take</param>
	/// <param name="player">player performing the action</param>
	/// <returns>progress bar associated with this action (can use this to interrupt progress). Null if
	/// progress was not started for some reason (such as already in progress for this action on the specified tile).</returns>
	public static ProgressBar ServerStartProgress(this IProgressAction progressAction, Vector3 worldTilePos,
		float timeForCompletion,
		GameObject player)
	{
		return UIManager._ServerStartProgress(progressAction, ActionTarget.Tile(worldTilePos), timeForCompletion, player);
	}

	/// <summary>
	/// Start a progress action targeting a specific object.
	/// Tries to create and begin animating a progress bar for a specific player. Returns null
	/// if progress did not begin for some reason.
	/// </summary>
	/// <param name="progressAction">progress action being performed</param>
	/// <param name="target">targeted object of the progress action</param>
	/// <param name="timeForCompletion">how long in seconds the action should take</param>
	/// <param name="player">player performing the action</param>
	/// <returns>progress bar associated with this action (can use this to interrupt progress). Null if
	/// progress was not started for some reason (such as already in progress for this action on the specified tile).</returns>
	public static ProgressBar ServerStartProgress(this IProgressAction progressAction, RegisterTile target,
		float timeForCompletion,
		GameObject player)
	{
		return UIManager._ServerStartProgress(progressAction, ActionTarget.Object(target), timeForCompletion, player);
	}
}
