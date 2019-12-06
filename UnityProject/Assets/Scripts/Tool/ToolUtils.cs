
using UnityEngine;

/// <summary>
/// Utilities for working with tools / materials. Respects the Tool component settings when performing actions.
/// </summary>
public static class ToolUtils
{
	/// <summary>
	/// Performs common tool usage logic, such as playing the correct sound.
	/// If item is not a tool, simply performs the progress action normally.
	/// </summary>
	/// <param name="performer">player using the tool</param>
	/// <param name="tool">tool being used</param>
	/// <param name="worldTilePos">tile position the action is being performed on</param>
	/// <param name="seconds">seconds taken to perform the action, 0 if it should be instant</param>
	/// <param name="progressCompleteAction">completion callback</param>
	/// <returns>progress bar spawned, null if progress did not start or this was instant</returns>
	public static ProgressBar ServerUseTool(GameObject performer, GameObject tool, Vector2 worldTilePos, float seconds, ProgressCompleteAction progressCompleteAction)
	{
		//check item attributes of used object to determine sound to play
		string soundName = null;
		var itemAttrs = tool.GetComponent<ItemAttributesV2>();
		if (itemAttrs != null)
		{
			if (itemAttrs.HasTrait(CommonTraits.Instance.Crowbar))
			{
				soundName = "Crowbar";
			}
			else if (itemAttrs.HasTrait(CommonTraits.Instance.Screwdriver))
			{
				soundName = "screwdriver#";
			}
			else if (itemAttrs.HasTrait(CommonTraits.Instance.Wirecutter))
			{
				soundName = "WireCutter";
			}
			else if (itemAttrs.HasTrait(CommonTraits.Instance.Wrench))
			{
				soundName = "Wrench";
			}
			else if (itemAttrs.HasTrait(CommonTraits.Instance.Welder))
			{
				soundName = "Weld";
			}
		}

		//check tool stats
		var toolStats = tool.GetComponent<Tool>();
		if (toolStats != null)
		{
			seconds /= toolStats.SpeedMultiplier;
		}

		if (seconds <= 0f)
		{
			SoundManager.PlayNetworkedAtPos("screwdriver#", worldTilePos, Random.Range(0.8f, 1.2f));
			return null;
		}
		else
		{
			var bar = UIManager.ServerStartProgress(ProgressAction.Construction, worldTilePos, seconds, progressCompleteAction, performer);
			if (bar != null && soundName != null)
			{
				SoundManager.PlayNetworkedAtPos("screwdriver#", worldTilePos, Random.Range(0.8f, 1.2f));
			}

			return bar;
		}
	}

	/// <summary>
	/// Performs common tool usage logic, such as playing the correct sound.
	/// If item is not a tool, simply performs the progress action normally.
	/// </summary>
	/// <param name="positionalHandApply">positional hand apply causing the tool usage</param>
	/// <param name="seconds">seconds taken to perform the action</param>
	/// <param name="progressCompleteAction">completion callback</param>
	/// <returns>progress bar spawned, null if progress did not start</returns>
	public static ProgressBar ServerUseTool(PositionalHandApply positionalHandApply, float seconds=0,
		ProgressCompleteAction progressCompleteAction=null)
	{
		return ServerUseTool(positionalHandApply.Performer, positionalHandApply.HandObject,
			positionalHandApply.WorldPositionTarget, seconds, progressCompleteAction);
	}

	/// <summary>
	/// Performs common tool usage logic, such as playing the correct sound.
	/// If item is not a tool, simply performs the progress action normally.
	/// </summary>
	/// <param name="handApply">hand apply causing the tool usage</param>
	/// <param name="seconds">seconds taken to perform the action</param>
	/// <param name="progressCompleteAction">completion callback</param>
	/// <returns>progress bar spawned, null if progress did not start</returns>
	public static ProgressBar ServerUseTool(HandApply handApply, float seconds=0,
		ProgressCompleteAction progressCompleteAction=null)
	{
		return ServerUseTool(handApply.Performer, handApply.HandObject,
			handApply.TargetObject.TileWorldPosition(), seconds, progressCompleteAction);
	}

	/// <summary>
	/// Performs common tool usage logic, such as playing the correct sound.
	/// If item is not a tool, simply performs the progress action normally.
	/// </summary>
	/// <param name="tileApply">tile apply causing the tool usage</param>
	/// <param name="seconds">seconds taken to perform the action</param>
	/// <param name="progressCompleteAction">completion callback</param>
	/// <returns>progress bar spawned, null if progress did not start</returns>
	public static ProgressBar ServerUseTool(TileApply tileApply, float seconds=0,
		ProgressCompleteAction progressCompleteAction=null)
	{
		return ServerUseTool(tileApply.Performer, tileApply.HandObject,
			tileApply.WorldPositionTarget, seconds, progressCompleteAction);
	}
}
