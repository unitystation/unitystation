
using System;
using System.Linq;
using Logs;
using UnityEngine;

public enum ActionInterruptionType
{
	MatrixRotation,
	MatrixMove,
	TargetDespawn,
	PerformerCuffed,
	PerformerOrTargetMoved,
	PerformerSlipped,
	PerformerUnconscious,
	PerformerDirection,
	ChangeToPerformerActiveSlot,
	WelderOff
}

/// <summary>
/// Progress action which covers the majority of common progress action use cases.
/// These should be used once per attempted progress action, don't reuse one for multiple
/// actions.
/// </summary>
public class StandardProgressAction : IProgressAction
{

	private readonly StandardProgressActionConfig progressActionConfig;



	//invoked on successful completion
	private readonly Action onCompletion;
	//invoked on interruption
	private readonly Action<ActionInterruptionType> onInterruption;

	//slot being used to perform the action, will be interrupted if slot contents change
	private ItemSlot usedSlot;
	//conscious state when beginning progress
	private ConsciousState initialConsciousState;
	//initial facing direction
	private OrientationEnum initialDirection;
	private PlayerScript playerScript;
	private StartProgressInfo startProgressInfo;
	//is this a cross matrix action
	private bool crossMatrix;
	//is this for a weld action? null if not
	private Welder welder;

	//so we don't try to reuse this object for multiple actions.
	private bool used;

	//for managing all the various events we subscribe to.
	private EventRegistry eventRegistry;

	/// <summary>
	/// Current progress bar.
	/// </summary>
	private ProgressBar ProgressBar => startProgressInfo?.ProgressBar;

	private StandardProgressAction(StandardProgressActionConfig progressActionConfig, Action onCompletion, Welder welder = null)
	{
		this.progressActionConfig = progressActionConfig;
		this.onCompletion = onCompletion;
		this.welder = welder;
	}

	private StandardProgressAction(StandardProgressActionConfig progressActionConfig, Action onCompletion,
		Action<ActionInterruptionType> onInterruption)
	{
		this.progressActionConfig = progressActionConfig;
		this.onCompletion = onCompletion;
		this.onInterruption = onInterruption;
	}

	/// <summary>
	/// Creates a new instance of a progress action with the indicated configuration and
	/// completion action.
	/// </summary>
	/// <param name="progressActionConfig">config</param>
	/// <param name="onCompletion">action to invoke server-side on successful completion (not invoked when interrupted)</param>
	/// <returns></returns>
	public static StandardProgressAction Create(StandardProgressActionConfig progressActionConfig,
		Action onCompletion)
	{
		return new StandardProgressAction(progressActionConfig, onCompletion);
	}

	/// <summary>
	/// Just like Create, but will auto-interrupt progress if the welder runs out of fuel.
	/// </summary>
	/// <param name="progressActionConfig"></param>
	/// <param name="onCompletion"></param>
	/// <returns></returns>
	public static StandardProgressAction CreateForWelder(StandardProgressActionConfig progressActionConfig,
		Action onCompletion, Welder welder)
	{
		return new StandardProgressAction(progressActionConfig, onCompletion, welder);
	}

	/// <summary>
	/// Creates a new instance of a progress action with the indicated configuration and
	/// completion action.
	/// </summary>
	/// <param name="progressActionConfig">config</param>
	/// <param name="onCompletion">action to invoke server-side on successful completion (not invoked when interrupted)</param>
	/// <param name="onInterrupted">action to invoke server-side when interrupted</param>
	/// <returns></returns>
	public static StandardProgressAction Create(StandardProgressActionConfig progressActionConfig,
		Action onCompletion, Action<ActionInterruptionType> onInterrupted)
	{
		return new StandardProgressAction(progressActionConfig, onCompletion, onInterrupted);
	}

	public bool OnServerStartProgress(StartProgressInfo info)
	{
		if (used)
		{
			Loggy.LogError("Attempted to reuse a StandardProgressAction that has already been used." +
			                      " Please create a new StandardProgressAction each time you start a new action.",
				Category.ProgressAction);
			return false;
		}
		startProgressInfo = info;
		//interrupt if hand contents are changed
		playerScript = info.Performer.Player().Script;

		if (!progressActionConfig.AllowMultiple)
		{

			try
			{
				//check if the performer is already doing this action type anywhere else
				var existingAction = UIManager.Instance.ProgressBars
					.Where(pb => pb.RegisterPlayer != null && pb.RegisterPlayer.gameObject == info.Performer)
					.FirstOrDefault(pb =>
					{
						if (pb.ServerProgressAction is StandardProgressAction standardProgressAction)
						{
							return standardProgressAction.progressActionConfig.StandardProgressActionType ==
							       progressActionConfig.StandardProgressActionType;
						}

						return false;
					});

				if (existingAction != null)
				{
					Loggy.LogTraceFormat(
						"Server cancelling progress bar {0} start because AllowMultiple=true and progress bar {1} " +
						" has same progress type and is already in progress.", Category.ProgressAction,
						info.ProgressBar.ID, existingAction.ID);
					return false;
				}
			}
			catch
			{
				Loggy.LogError(
					"Something terrible happened to ProgressBars but we have recovered.", Category.ProgressAction);
				return false;
			}
		}

		//check if there is already progress of this type at this location by this player
		var targetParent = info.Target.TargetMatrixInfo.Objects;
		var existingBar = UIManager.Instance.ProgressBars
			.Where(pb => pb.RegisterPlayer != null && pb.RegisterPlayer.gameObject == info.Performer)
			.Where(pb => pb.transform.parent == targetParent)
			.FirstOrDefault(pb => Vector3.Distance(pb.transform.localPosition, info.Target.TargetLocalPosition) < 0.1);
		if (existingBar != null)
		{
			//progress already started by this player at this position
			Loggy.LogTraceFormat("Server cancelling progress bar {0} start because progress bar {1} " +
			                      " has same progress type and is already in progress at the target location by this player.",
				Category.ProgressAction, info.ProgressBar.ID, existingBar.ID);
			return false;
		}

		//Can this progress bar be interrupted by movement?
		if (progressActionConfig.AllowMovement == false)
		{
			//is this cross matrix? if so, don't start progress if either matrix is moving
			var performerMatrix = playerScript.RegisterPlayer.Matrix;
			crossMatrix = performerMatrix != info.Target.TargetMatrixInfo.Matrix;
			if (crossMatrix && (performerMatrix.IsMovingServer || info.Target.TargetMatrixInfo.Matrix.IsMovingServer))
			{
				//progress already started by this player at this position
				Loggy.LogTraceFormat("Server cancelling progress bar {0} start because it is cross matrix and one of" +
				                      " the matrices is moving.",
					Category.ProgressAction, info.ProgressBar.ID);
				return false;
			}
		}


		//we are going to start progress, so set up the hooks
		RegisterHooks();

		return true;
	}

	private void RegisterHooks()
	{
		eventRegistry?.UnregisterAll();
		eventRegistry = new EventRegistry();
		//interrupt if welder turns off
		if (welder)
		{
			eventRegistry.Register(welder.OnWelderOffServer, OnWelderOff);
		}

		if (startProgressInfo.Target.IsObject)
		{
			//if targeting an object, interrupt if object moves away
			eventRegistry.Register(startProgressInfo.Target.Target.OnLocalPositionChangedServer, OnLocalPositionChanged);

			//interrupt if target is despawned
			eventRegistry.Register(startProgressInfo.Target.Target.OnDespawnedServer, OnDespawned);
		}
		//interrupt if active hand slot changes
		var activeSlot = playerScript.DynamicItemStorage?.GetActiveHandSlot();
		eventRegistry.Register(activeSlot?.OnSlotContentsChangeServer, OnSlotContentsChanged);
		usedSlot = activeSlot;
		//interrupt if cuffed
		eventRegistry.Register(playerScript.playerMove.OnCuffChangeServer, OnCuffChange);
		//interrupt if slipped
		eventRegistry.Register(playerScript.RegisterPlayer.OnSlipChangeServer, OnSlipChange);
		//interrupt if conscious state changes
		eventRegistry.Register(playerScript.playerHealth.OnConsciousStateChangeServer, OnConsciousStateChange);
		initialConsciousState = playerScript.playerHealth.ConsciousState;
		//interrupt if player moves at all
		if (progressActionConfig.AllowMovement == false)
		{
			eventRegistry.Register(playerScript.RegisterPlayer.OnLocalPositionChangedServer, OnLocalPositionChanged);
		}
		//interrupt if player turns away and turning is not allowed
		eventRegistry.Register(playerScript.PlayerDirectional.OnRotationChange, OnDirectionChanged);
		initialDirection = playerScript.PlayerDirectional.CurrentDirection;
		//interrupt if tile is on different matrix and either matrix moves / rotates
		if (crossMatrix)
		{
			if (startProgressInfo.Target.TargetMatrixInfo.IsMovable)
			{
				eventRegistry.Register(startProgressInfo.Target.TargetMatrixInfo.MatrixMove.MatrixMoveEvents.OnStartMovementServer, OnMatrixStartMove);
				eventRegistry.Register(startProgressInfo.Target.TargetMatrixInfo.MatrixMove.MatrixMoveEvents.OnRotate, OnMatrixRotate);
			}

			var performerMatrix = playerScript.RegisterPlayer.Matrix;
			if (performerMatrix.IsMovable)
			{
				eventRegistry.Register(performerMatrix.MatrixMove.MatrixMoveEvents.OnStartMovementServer, OnMatrixStartMove);
				eventRegistry.Register(performerMatrix.MatrixMove.MatrixMoveEvents.OnRotate, OnMatrixRotate);
			}
		}
	}

	public bool OnServerContinueProgress(InProgressInfo info)
	{
		//all situations which stop progress are checked using
		//event hooks for better performance.
		return true;
	}

	public void OnServerEndProgress(EndProgressInfo info)
	{
		used = true;
		//only interrupt other progress bars if we were completed
		if (info.WasCompleted && progressActionConfig.InterruptsOverlapping)
		{
			//interrupt other progress bars of the same action type on the same location
			var existingBars = UIManager.Instance.ProgressBars
				.Where(pb => ProgressBar != null && pb != null && pb != ProgressBar)
				.Where(pb => pb.ServerProgressAction is StandardProgressAction)
				.Where(pb =>
					((StandardProgressAction) pb.ServerProgressAction).progressActionConfig.StandardProgressActionType == progressActionConfig.StandardProgressActionType)
				.Where(pb => pb.transform.parent == ProgressBar.transform.parent)
				.Where(pb => Vector3.Distance(pb.transform.localPosition, startProgressInfo.Target.TargetLocalPosition) < 0.1)
				.ToList();

			foreach (var existingBar in existingBars)
			{
				Loggy.LogTraceFormat("Server interrupting progress bar {0} because progress bar {1} finished " +
				                      "on same tile", Category.ProgressAction, existingBar.ID, ProgressBar.ID);
				existingBar.ServerInterruptProgress();
			}
		}

		//unregister from all hooks
		eventRegistry.UnregisterAll();

		if (info.WasCompleted)
		{
			onCompletion?.Invoke();
		}
	}

	private void InterruptProgress(string reason, ActionInterruptionType interruptionType)
	{
		if(progressActionConfig.AllowMovement == true) return;
		Loggy.LogTraceFormat("Server progress bar {0} interrupted: {1}.", Category.ProgressAction,
			ProgressBar.ID, reason);
		ProgressBar.ServerInterruptProgress();
		onInterruption?.Invoke(interruptionType);
	}

	private void OnMatrixRotate(MatrixRotationInfo arg0)
	{
		if(progressActionConfig.AllowMovement == true) return;
		InterruptProgress("cross-matrix and target or performer matrix rotated", ActionInterruptionType.MatrixRotation);
	}

	private bool CanPlayerStillProgress()
	{
		//note: doesn't check cross matrix situations.
		return playerScript.playerHealth.ConsciousState == initialConsciousState &&
		       (progressActionConfig.AllowDuringCuff || playerScript.playerMove.IsCuffed == false) &&
		       playerScript.RegisterPlayer.IsSlippingServer == false &&
			   playerScript.PlayerNetworkActions.IsRolling == false &&
		       (progressActionConfig.AllowTurning || playerScript.PlayerDirectional.CurrentDirection != initialDirection) &&
		       playerScript.PlayerSync.IsMoving == false &&
		       //make sure we're still in range
		       Validations.IsInReachDistanceByPositions(playerScript.RegisterPlayer.WorldPositionServer,
			       startProgressInfo.Target.TargetWorldPosition);
	}

	private void OnWelderOff()
	{
		InterruptProgress("welder off", ActionInterruptionType.WelderOff);
	}

	private void OnSlotContentsChanged()
	{
		InterruptProgress("performer's active slot contents changed",
			ActionInterruptionType.ChangeToPerformerActiveSlot);
	}

	private void OnMatrixStartMove()
	{
		InterruptProgress("cross-matrix and performer or target matrix started moving",
			ActionInterruptionType.MatrixMove);
	}

	private void OnLocalPositionChanged(Vector3Int arg0)
	{
		//if player or target moves at all, interrupt
		InterruptProgress("performer or target moved",ActionInterruptionType.PerformerOrTargetMoved);
	}

	private void OnDespawned()
	{
		InterruptProgress("target was despawned", ActionInterruptionType.TargetDespawn);
	}

	private void OnConsciousStateChange(ConsciousState oldState, ConsciousState newState)
	{
		if (!CanPlayerStillProgress()) InterruptProgress("performer not conscious enough",
			ActionInterruptionType.PerformerUnconscious);
	}

	private void OnSlipChange(bool wasSlipped, bool nowSlipped)
	{
		if (!CanPlayerStillProgress()) InterruptProgress("performer slipped",
			ActionInterruptionType.PerformerSlipped);
	}

	private void OnCuffChange(bool wasCuffed, bool nowCuffed)
	{
		if (!CanPlayerStillProgress()) InterruptProgress("performer cuffed",
			ActionInterruptionType.PerformerCuffed);
	}


	private void OnDirectionChanged(OrientationEnum arg0)
	{
		if (!CanPlayerStillProgress()) InterruptProgress("performer direction changed",
			ActionInterruptionType.PerformerDirection);
	}
}
