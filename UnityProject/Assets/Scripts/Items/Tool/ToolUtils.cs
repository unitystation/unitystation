using System;
using UnityEngine;
using Items;
using Random = UnityEngine.Random;
using AddressableReferences;
using HealthV2;
using Messages.Server.SoundMessages;


/// <summary>
/// Utilities for working with tools / materials. Respects the Tool component settings when performing actions.
/// </summary>
public static class ToolUtils
{
	private static readonly StandardProgressActionConfig ProgressConfig =
		new StandardProgressActionConfig(StandardProgressActionType.Construction, true);

	private static readonly System.Random RNG = new System.Random();

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
	/// <param name="performerFailMessage">message to show performer when action completes unsuccessfully.</param>
	/// <param name="othersFailMessage">message to show others when action completes unsuccessfully.</param>
	/// <param name="onFailComplete">called when action is completed unsuccessfully.</param>
	/// <param name="playSound">Whether to play default tool sound</param>
	public static void ServerUseToolWithActionMessages(GameObject performer, GameObject tool, ActionTarget actionTarget,
		float seconds, string performerStartActionMessage, string othersStartActionMessage,
		string performerFinishActionMessage,
		string othersFinishActionMessage, Action onSuccessfulCompletion, string performerFailMessage = "",
		string othersFailMessage = "", Action onFailComplete = null, bool playSound = true)
	{
		void ProgressComplete()
		{
			var Tooltool = tool.GetComponent<Tool>();
			if (Tooltool != null)
			{
				if (Tooltool.PercentageChance < 100)
				{
					int NOWRNG = RNG.Next(0, 100);
					if (NOWRNG > Tooltool.PercentageChance)
					{
						Chat.AddActionMsgToChat(performer, performerFailMessage,
							othersFailMessage);
						onFailComplete?.Invoke();
						return;
					}
				}
			}

			Chat.AddActionMsgToChat(performer, performerFinishActionMessage,
				othersFinishActionMessage);
			onSuccessfulCompletion.Invoke();
		}

		//only play the start action message if progress actually started
		if (ServerUseTool(performer, tool, actionTarget, seconds, ProgressComplete, playSound))
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
	/// <param name="performerFailMessage">message to show performer when action completes unsuccessfully.</param>
	/// <param name="othersFailMessage">message to show others when action completes unsuccessfully.</param>
	/// <param name="onFailComplete">called when action is completed unsuccessfully.</param>
	/// <param name="playSound">Whether to play default tool sound</param>
	public static void ServerUseToolWithActionMessages(HandApply handApply,
		float seconds, string performerStartActionMessage, string othersStartActionMessage,
		string performerFinishActionMessage,
		string othersFinishActionMessage, Action onSuccessfulCompletion, string performerFailMessage = "",
		string othersFailMessage = "", Action onFailComplete = null, bool playSound = true)
	{
		ServerUseToolWithActionMessages(handApply.Performer, handApply.HandObject,
			ActionTarget.Object(handApply.TargetObject.RegisterTile()), seconds, performerStartActionMessage,
			othersStartActionMessage,
			performerFinishActionMessage, othersFinishActionMessage, onSuccessfulCompletion, performerFailMessage,
			othersFailMessage, onFailComplete, playSound);
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
	/// <param name="performerFailMessage">message to show performer when action completes unsuccessfully.</param>
	/// <param name="othersFailMessage">message to show others when action completes unsuccessfully.</param>
	/// <param name="onFailComplete">called when action is completed unsuccessfully.</param>
	/// <param name="playSound">Whether to play default tool sound</param>
	public static void ServerUseToolWithActionMessages(TileApply tileApply,
		float seconds, string performerStartActionMessage, string othersStartActionMessage,
		string performerFinishActionMessage,
		string othersFinishActionMessage, Action onSuccessfulCompletion, string performerFailMessage = "",
		string othersFailMessage = "", Action onFailComplete = null, bool playSound = true)
	{
		ServerUseToolWithActionMessages(tileApply.Performer, tileApply.HandObject,
			ActionTarget.Tile(tileApply.WorldPositionTarget), seconds, performerStartActionMessage,
			othersStartActionMessage,
			performerFinishActionMessage, othersFinishActionMessage, onSuccessfulCompletion, performerFailMessage, othersFailMessage, onFailComplete, playSound);
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
	/// <param name="performerFailMessage">message to show performer when action completes unsuccessfully.</param>
	/// <param name="othersFailMessage">message to show others when action completes unsuccessfully.</param>
	/// <param name="onFailComplete">called when action is completed unsuccessfully.</param>
	/// <param name="playSound">Whether to play default tool sound</param>
	public static void ServerUseToolWithActionMessages(PositionalHandApply handApply,
		float seconds, string performerStartActionMessage, string othersStartActionMessage,
		string performerFinishActionMessage,
		string othersFinishActionMessage, Action onSuccessfulCompletion, string performerFailMessage = "",
		string othersFailMessage = "", Action onFailComplete = null, bool playSound = true)
	{
		ServerUseToolWithActionMessages(handApply.Performer, handApply.HandObject,
			ActionTarget.Tile(handApply.WorldPositionTarget), seconds, performerStartActionMessage,
			othersStartActionMessage,
			performerFinishActionMessage, othersFinishActionMessage, onSuccessfulCompletion, performerFailMessage, othersFailMessage, onFailComplete, playSound);
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
	/// <param name="playSound">Whether to play default tool sound</param>
	/// <returns>progress bar spawned, null if progress did not start or this was instant</returns>
	public static ProgressBar ServerUseTool(GameObject performer, GameObject tool, ActionTarget actionTarget,
		float seconds, Action progressCompleteAction, bool playSound = true)
	{
		//check tool stats
		var toolStats = tool.GetComponent<Tool>();
		if (toolStats != null)
		{
			seconds /= toolStats.SpeedMultiplier;
		}

		if (seconds <= 0f)
		{
			if (playSound)
			{
				ServerPlayToolSound(tool, actionTarget.TargetWorldPosition, performer);
			}

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
				performer.GetComponent<LivingHealthMasterBase>()?.TryFlash(seconds);
				bar = StandardProgressAction.CreateForWelder(ProgressConfig, progressCompleteAction, welder)
					.ServerStartProgress(actionTarget, seconds, performer);
			}
			else
			{
				bar = StandardProgressAction.Create(ProgressConfig, progressCompleteAction)
					.ServerStartProgress(actionTarget, seconds, performer);
			}

			if (bar != null && playSound)
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

		AddressableAudioSource soundName = null;

		if (tool.TryGetComponent(out ToolSwapComponent toolSwap))
		{
			soundName = toolSwap.CurrentState.usingSound;
		}
		else if (tool.TryGetComponent(out ItemAttributesV2 itemAttrs))
		{
			if (itemAttrs.HasTrait(CommonTraits.Instance.Crowbar))
			{
				soundName = CommonSounds.Instance.Crowbar;
			}
			else if (itemAttrs.HasTrait(CommonTraits.Instance.Screwdriver))
			{
				soundName = CommonSounds.Instance.screwdriver;
			}
			else if (itemAttrs.HasTrait(CommonTraits.Instance.Wirecutter))
			{
				soundName = CommonSounds.Instance.WireCutter;
			}
			else if (itemAttrs.HasTrait(CommonTraits.Instance.Wrench))
			{
				soundName = CommonSounds.Instance.Wrench;
			}
			else if (itemAttrs.HasTrait(CommonTraits.Instance.Welder))
			{
				soundName = CommonSounds.Instance.Weld;
			}
			else if (itemAttrs.HasTrait(CommonTraits.Instance.Shovel))
			{
				soundName = CommonSounds.Instance.Shovel;
			}
			else if (itemAttrs.HasTrait(CommonTraits.Instance.AirlockPainter))
			{
				soundName = CommonSounds.Instance.AirlockPainter;
			}
		}

		if (soundName != null)
		{
			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: Random.Range(0.8f, 1.2f));
			SoundManager.PlayNetworkedAtPos(soundName, worldTilePos, audioSourceParameters, sourceObj: owner);
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
	/// Plays the tool sound for the used objet at the performer's position.
	/// </summary>
	/// <param name="inventoryApply"></param>
	public static void ServerPlayToolSound(InventoryApply inventoryApply)
	{
		ServerPlayToolSound(inventoryApply.UsedObject, inventoryApply.Performer.TileWorldPosition(),
			inventoryApply.Performer);
	}

	/// <summary>
	/// Performs common tool usage logic, such as playing the correct sound.
	/// If item is not a tool, simply performs the progress action normally.
	/// </summary>
	/// <param name="positionalHandApply">positional hand apply causing the tool usage</param>
	/// <param name="seconds">seconds taken to perform the action, 0 for instant.</param>
	/// <param name="progressCompleteAction">completion callback</param>
	/// <param name="playSound">Whether to play default tool sound</param>
	/// <returns>progress bar spawned, null if progress did not start</returns>
	public static ProgressBar ServerUseTool(PositionalHandApply positionalHandApply, float seconds = 0,
		Action progressCompleteAction = null, bool playSound = true)
	{
		return ServerUseTool(positionalHandApply.Performer, positionalHandApply.HandObject,
			ActionTarget.Tile(positionalHandApply.WorldPositionTarget), seconds, progressCompleteAction, playSound);
	}

	/// <summary>
	/// Performs common tool usage logic, such as playing the correct sound.
	/// If item is not a tool, simply performs the progress action normally.
	/// </summary>
	/// <param name="handApply">hand apply causing the tool usage</param>
	/// <param name="seconds">seconds taken to perform the action, 0 for instant.</param>
	/// <param name="progressCompleteAction">completion callback</param>
	/// <param name="playSound">Whether to play default tool sound</param>
	/// <returns>progress bar spawned, null if progress did not start</returns>
	public static ProgressBar ServerUseTool(HandApply handApply, float seconds = 0,
		Action progressCompleteAction = null, bool playSound = true)
	{
		return ServerUseTool(handApply.Performer, handApply.HandObject,
			ActionTarget.Object(handApply.TargetObject.RegisterTile()), seconds, progressCompleteAction, playSound);
	}

	/// <summary>
	/// Performs common tool usage logic, such as playing the correct sound.
	/// If item is not a tool, simply performs the progress action normally.
	/// </summary>
	/// <param name="tileApply">tile apply causing the tool usage</param>
	/// <param name="seconds">seconds taken to perform the action, 0 for instant.</param>
	/// <param name="progressCompleteAction">completion callback</param>
	/// <param name="playSound">Whether to play default tool sound</param>
	/// <returns>progress bar spawned, null if progress did not start</returns>
	public static ProgressBar ServerUseTool(TileApply tileApply, float seconds = 0,
		Action progressCompleteAction = null, bool playSound = true)
	{
		return ServerUseTool(tileApply.Performer, tileApply.HandObject,
			ActionTarget.Tile(tileApply.WorldPositionTarget), seconds, progressCompleteAction, playSound);
	}
}