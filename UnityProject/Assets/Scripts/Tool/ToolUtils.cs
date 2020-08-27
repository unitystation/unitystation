
using System;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Utilities for working with tools / materials. Respects the Tool component settings when performing actions.
/// </summary>
public static class ToolUtils
{
	private static readonly StandardProgressActionConfig ProgressConfig =
		new StandardProgressActionConfig(StandardProgressActionType.Construction, true);

	/// <summary>
	/// Performs common tool usage logic and also sends start / end action messages, and invokes a callback on success.
	/// </summary>
	/// <param name="performer">player using the tool</param>
	/// <param name="tool">tool being used</param>
	/// <param name="actionTarget">target of the action</param>
	/// <param name="seconds">seconds taken to perform the action, 0 if it should be instant</param>
	/// <param name="performerStartActionMessage">message to show performer when action begins.</param>
	/// <param name="othersStartActionMessage">message to show others when action begins.</param>
	/// <param name="performerFinishActionMessage">message to show performer when action completes successfully.</param>
	/// <param name="othersFinishActionMessage">message to show others when action completes successfully.</param>
	/// <param name="onSuccessfulCompletion">called when action is completed</param>
	public static void ServerUseToolWithActionMessages(GameObject performer, GameObject tool, ActionTarget actionTarget,
		float seconds, string performerStartActionMessage, string othersStartActionMessage, string performerFinishActionMessage,
		string othersFinishActionMessage, Action onSuccessfulCompletion)
	{
		void ProgressComplete()
		{
			Chat.AddActionMsgToChat(performer, performerFinishActionMessage,
				othersFinishActionMessage);
			onSuccessfulCompletion.Invoke();
		}

		//only play the start action message if progress actually started
		if (ServerUseTool(performer, tool, actionTarget, seconds, ProgressComplete))
		{
			Chat.AddActionMsgToChat(performer, performerStartActionMessage,
				othersStartActionMessage);
		}
	}

	/// <summary>
	/// Performs common tool usage logic and also sends start / end action messages, and invokes a callback on success.
	/// </summary>
	/// <param name="handApply">interaction causing the tool use</param>
	/// <param name="seconds">seconds taken to perform the action, 0 if it should be instant</param>
	/// <param name="performerStartActionMessage">message to show performer when action begins.</param>
	/// <param name="othersStartActionMessage">message to show others when action begins.</param>
	/// <param name="performerFinishActionMessage">message to show performer when action completes successfully.</param>
	/// <param name="othersFinishActionMessage">message to show others when action completes successfully.</param>
	/// <param name="onSuccessfulCompletion">called when action is completed</param>
	public static void ServerUseToolWithActionMessages(HandApply handApply,
		float seconds, string performerStartActionMessage, string othersStartActionMessage,
		string performerFinishActionMessage,
		string othersFinishActionMessage, Action onSuccessfulCompletion)
	{
		ServerUseToolWithActionMessages(handApply.Performer, handApply.HandObject,
			ActionTarget.Object(handApply.TargetObject.RegisterTile()), seconds, performerStartActionMessage, othersStartActionMessage,
			performerFinishActionMessage, othersFinishActionMessage, onSuccessfulCompletion);
	}

	/// <summary>
	/// Performs common tool usage logic and also sends start / end action messages, and invokes a callback on success.
	/// </summary>
	/// <param name="tileApply">interaction causing the tool use</param>
	/// <param name="seconds">seconds taken to perform the action, 0 if it should be instant</param>
	/// <param name="performerStartActionMessage">message to show performer when action begins.</param>
	/// <param name="othersStartActionMessage">message to show others when action begins.</param>
	/// <param name="performerFinishActionMessage">message to show performer when action completes successfully.</param>
	/// <param name="othersFinishActionMessage">message to show others when action completes successfully.</param>
	/// <param name="onSuccessfulCompletion">called when action is completed</param>
	public static void ServerUseToolWithActionMessages(TileApply tileApply,
		float seconds, string performerStartActionMessage, string othersStartActionMessage,
		string performerFinishActionMessage,
		string othersFinishActionMessage, Action onSuccessfulCompletion)
	{
		ServerUseToolWithActionMessages(tileApply.Performer, tileApply.HandObject,
			ActionTarget.Tile(tileApply.WorldPositionTarget), seconds, performerStartActionMessage, othersStartActionMessage,
			performerFinishActionMessage, othersFinishActionMessage, onSuccessfulCompletion);
	}

	/// <summary>
	/// Performs common tool usage logic and also sends start / end action messages, and invokes a callback on success.
	/// </summary>
	/// <param name="handApply">interaction causing the tool use</param>
	/// <param name="seconds">seconds taken to perform the action, 0 if it should be instant</param>
	/// <param name="performerStartActionMessage">message to show performer when action begins.</param>
	/// <param name="othersStartActionMessage">message to show others when action begins.</param>
	/// <param name="performerFinishActionMessage">message to show performer when action completes successfully.</param>
	/// <param name="othersFinishActionMessage">message to show others when action completes successfully.</param>
	/// <param name="onSuccessfulCompletion">called when action is completed</param>
	public static void ServerUseToolWithActionMessages(PositionalHandApply handApply,
		float seconds, string performerStartActionMessage, string othersStartActionMessage,
		string performerFinishActionMessage,
		string othersFinishActionMessage, Action onSuccessfulCompletion)
	{
		ServerUseToolWithActionMessages(handApply.Performer, handApply.HandObject,
			ActionTarget.Tile(handApply.WorldPositionTarget), seconds, performerStartActionMessage, othersStartActionMessage,
			performerFinishActionMessage, othersFinishActionMessage, onSuccessfulCompletion);
	}

	/// <summary>
	/// Performs common tool usage logic, such as playing the correct sound.
	/// If item is not a tool, simply performs the progress action normally.
	/// </summary>
	/// <param name="performer">player using the tool</param>
	/// <param name="tool">tool being used</param>
	/// <param name="actionTarget">target of the action</param>
	/// <param name="seconds">seconds taken to perform the action, 0 if it should be instant</param>
	/// <param name="progressCompleteAction">completion callback (will also be called instantly if completion is instant)</param>
	/// <returns>progress bar spawned, null if progress did not start or this was instant</returns>
	public static ProgressBar ServerUseTool(GameObject performer, GameObject tool, ActionTarget actionTarget, float seconds, Action progressCompleteAction)
	{
		//check tool stats
		var toolStats = tool.GetComponent<Tool>();
		if (toolStats != null)
		{
			seconds /= toolStats.SpeedMultiplier;
		}

		if (seconds <= 0f)
		{
			ServerPlayToolSound(tool, actionTarget.TargetWorldPosition, performer);
			// Check for null as ServerUseTool(interaction) accepts null Action
			if (progressCompleteAction != null) progressCompleteAction.Invoke();
			return null;
		}
		else
		{
			var welder = tool.GetComponent<Welder>();
			ProgressBar bar;
			if (welder != null)
			{
				bar = StandardProgressAction.CreateForWelder(ProgressConfig, progressCompleteAction, welder)
					.ServerStartProgress(actionTarget, seconds, performer);
			}
			else
			{
				bar = StandardProgressAction.Create(ProgressConfig, progressCompleteAction)
					.ServerStartProgress(actionTarget, seconds, performer);
			}

			if (bar != null)
			{
				ServerPlayToolSound(tool, actionTarget.TargetWorldPosition, performer);
			}

			return bar;
		}
	}

	/// <summary>
	/// Places the correct sound for the indicated tool at the indicated position.
	/// Plays no sound if it has no corresponding sound.
	/// </summary>
	/// <param name="tool"></param>
	/// <param name="worldTilePos"></param>
	public static void ServerPlayToolSound(GameObject tool, Vector2 worldTilePos, GameObject owner = null)
	{
		if (tool == null) return;
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
			else if (itemAttrs.HasTrait(CommonTraits.Instance.Shovel))
			{
				soundName = "Shovel";
			}
		}

		if (soundName != null)
		{
			SoundManager.PlayNetworkedAtPos(soundName, worldTilePos, Random.Range(0.8f, 1.2f), sourceObj: owner);
		}
	}

	/// <summary>
	/// Plays the tool sound for the used object at the target position
	/// </summary>
	/// <param name="handApply"></param>
	public static void ServerPlayToolSound(HandApply handApply)
	{
		ServerPlayToolSound(handApply.UsedObject, handApply.TargetObject.TileWorldPosition(), handApply.Performer);
	}

	/// <summary>
	/// Plays the tool sound for the used object at the target position
	/// </summary>
	/// <param name="handApply"></param>
	public static void ServerPlayToolSound(PositionalHandApply handApply)
	{
		ServerPlayToolSound(handApply.UsedObject, handApply.WorldPositionTarget, handApply.Performer);
	}

	/// <summary>
	/// Plays the tool sound for the used object at the target position
	/// </summary>
	/// <param name="tileApply"></param>
	public static void ServerPlayToolSound(TileApply tileApply)
	{
		ServerPlayToolSound(tileApply.UsedObject, tileApply.WorldPositionTarget, tileApply.Performer);
	}

	/// <summary>
	/// Performs common tool usage logic, such as playing the correct sound.
	/// If item is not a tool, simply performs the progress action normally.
	/// </summary>
	/// <param name="positionalHandApply">positional hand apply causing the tool usage</param>
	/// <param name="seconds">seconds taken to perform the action, 0 for instant.</param>
	/// <param name="progressCompleteAction">completion callback</param>
	/// <returns>progress bar spawned, null if progress did not start</returns>
	public static ProgressBar ServerUseTool(PositionalHandApply positionalHandApply, float seconds=0,
		Action progressCompleteAction=null)
	{
		return ServerUseTool(positionalHandApply.Performer, positionalHandApply.HandObject,
			ActionTarget.Tile(positionalHandApply.WorldPositionTarget), seconds, progressCompleteAction);
	}

	/// <summary>
	/// Performs common tool usage logic, such as playing the correct sound.
	/// If item is not a tool, simply performs the progress action normally.
	/// </summary>
	/// <param name="handApply">hand apply causing the tool usage</param>
	/// <param name="seconds">seconds taken to perform the action, 0 for instant.</param>
	/// <param name="progressCompleteAction">completion callback</param>
	/// <returns>progress bar spawned, null if progress did not start</returns>
	public static ProgressBar ServerUseTool(HandApply handApply, float seconds=0,
		Action progressCompleteAction=null)
	{
		return ServerUseTool(handApply.Performer, handApply.HandObject,
			ActionTarget.Object(handApply.TargetObject.RegisterTile()), seconds, progressCompleteAction);
	}

	/// <summary>
	/// Performs common tool usage logic, such as playing the correct sound.
	/// If item is not a tool, simply performs the progress action normally.
	/// </summary>
	/// <param name="tileApply">tile apply causing the tool usage</param>
	/// <param name="seconds">seconds taken to perform the action, 0 for instant.</param>
	/// <param name="progressCompleteAction">completion callback</param>
	/// <returns>progress bar spawned, null if progress did not start</returns>
	public static ProgressBar ServerUseTool(TileApply tileApply, float seconds=0,
		Action progressCompleteAction=null)
	{
		return ServerUseTool(tileApply.Performer, tileApply.HandObject,
			ActionTarget.Tile(tileApply.WorldPositionTarget), seconds, progressCompleteAction);
	}
}
