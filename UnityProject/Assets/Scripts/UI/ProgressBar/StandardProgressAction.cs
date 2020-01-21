
using System;
using System.Linq;
using UnityEngine;

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

	//slot being used to perform the action, will be interrupted if slot contents change
	private ItemSlot usedSlot;
	//conscious state when beginning progress
	private ConsciousState initialConsciousState;
	//initial facing direction
	private Orientation initialDirection;
	private PlayerScript playerScript;
	private StartProgressInfo startProgressInfo;
	//is this a cross matrix action
	private bool crossMatrix;
	//is this for a weld action? null if not
	private Welder welder;

	//so we don't try to reuse this object for multiple actions.
	private bool used;

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

	public bool OnServerStartProgress(StartProgressInfo info)
	{
		if (used)
		{
			Logger.LogError("Attempted to reuse a StandardProgressAction that has already been used." +
			                      " Please create a new StandardProgressAction each time you start a new action.",
				Category.Interaction);
			return false;
		}
		startProgressInfo = info;
		//interrupt if hand contents are changed
		playerScript = info.Performer.Player().Script;

		if (!progressActionConfig.AllowMultiple)
		{
			//check if the performer is already doing this action type anywhere else
			var existingAction = UIManager.Instance.ProgressBars
				.Where(pb => pb.RegisterPlayer.gameObject == info.Performer)
				.FirstOrDefault(pb =>
				{
					if (pb.ServerProgressAction is StandardProgressAction standardProgressAction)
					{
						return standardProgressAction.progressActionConfig.StandardProgressActionType == progressActionConfig.StandardProgressActionType;
					}

					return false;
				});
			if (existingAction != null)
			{
				Logger.LogTraceFormat("Server cancelling progress bar {0} start because AllowMultiple=true and progress bar {1} " +
				                      " has same progress type and is already in progress.", Category.ProgressAction, info.ProgressBar.ID, existingAction.ID);
				return false;
			}
		}

		//check if there is already progress of this type at this location by this player
		var targetParent = info.Target.TargetMatrixInfo.Objects;
		var existingBar = UIManager.Instance.ProgressBars
			.Where(pb => pb.RegisterPlayer.gameObject == info.Performer)
			.Where(pb => pb.transform.parent == targetParent)
			.FirstOrDefault(pb => Vector3.Distance(pb.transform.localPosition, info.Target.TargetLocalPosition) < 0.1);
		if (existingBar != null)
		{
			//progress already started by this player at this position
			Logger.LogTraceFormat("Server cancelling progress bar {0} start because progress bar {1} " +
			                      " has same progress type and is already in progress at the target location by this player.",
				Category.ProgressAction, info.ProgressBar.ID, existingBar.ID);
			return false;
		}

		//is this cross matrix? if so, don't start progress if either matrix is moving
		var performerMatrix = playerScript.registerTile.Matrix;
		crossMatrix = performerMatrix != info.Target.TargetMatrixInfo.Matrix;
		if (crossMatrix && (performerMatrix.IsMovingServer || info.Target.TargetMatrixInfo.Matrix.IsMovingServer))
		{
			//progress already started by this player at this position
			Logger.LogTraceFormat("Server cancelling progress bar {0} start because it is cross matrix and one of" +
			                      " the matrices is moving.",
				Category.ProgressAction, info.ProgressBar.ID);
			return false;
		}

		//we are going to start progress, so set up the hooks
		RegisterHooks();

		return true;
	}

	private void RegisterHooks()
	{
		//interrupt if welder turns off
		if (welder)
		{
			welder.OnWelderOffServer.AddListener(OnWelderOff);
		}
		//if targeting an object, interrupt if object moves away
		if (startProgressInfo.Target.IsObject)
		{
			startProgressInfo.Target.Target.OnLocalPositionChangedServer.AddListener(OnLocalPositionChanged);
		}
		//interrupt if active hand slot changes
		var activeSlot = playerScript.ItemStorage.GetActiveHandSlot();
		activeSlot.OnSlotContentsChangeServer.AddListener(OnSlotContentsChanged);
		usedSlot = activeSlot;
		//interrupt if cuffed
		playerScript.playerMove.OnCuffChangeServer.AddListener(OnCuffChange);
		//interrupt if slipped
		playerScript.registerTile.OnSlipChangeServer.AddListener(OnSlipChange);
		//interrupt if conscious state changes
		playerScript.playerHealth.OnConsciousStateChangeServer.AddListener(OnConsciousStateChange);
		initialConsciousState = playerScript.playerHealth.ConsciousState;
		//interrupt if player moves at all
		playerScript.registerTile.OnLocalPositionChangedServer.AddListener(OnLocalPositionChanged);
		//interrupt if player turns away and turning is not allowed
		playerScript.playerDirectional.OnDirectionChange.AddListener(OnDirectionChanged);
		initialDirection = playerScript.playerDirectional.CurrentDirection;
		//interrupt if tile is on different matrix and either matrix moves / rotates
		if (crossMatrix)
		{
			if (startProgressInfo.Target.TargetMatrixInfo.MatrixMove != null)
			{
				startProgressInfo.Target.TargetMatrixInfo.MatrixMove.MatrixMoveEvents.OnStartMovementServer.AddListener(OnMatrixStartMove);
				startProgressInfo.Target.TargetMatrixInfo.MatrixMove.MatrixMoveEvents.OnRotate.AddListener(OnTargetMatrixRotate);
			}

			var performerMatrix = playerScript.registerTile.Matrix;
			if (performerMatrix.MatrixMove != null)
			{
				startProgressInfo.Target.TargetMatrixInfo.MatrixMove.MatrixMoveEvents.OnStartMovementServer.AddListener(OnMatrixStartMove);
				startProgressInfo.Target.TargetMatrixInfo.MatrixMove.MatrixMoveEvents.OnRotate.AddListener(OnTargetMatrixRotate);
			}

		}
	}

	private void UnregisterHooks()
	{
		if (welder)
		{
			welder.OnWelderOffServer.RemoveListener(OnWelderOff);
		}
		if (startProgressInfo.Target.IsObject)
		{
			startProgressInfo.Target.Target.OnLocalPositionChangedServer.RemoveListener(OnLocalPositionChanged);
		}
		if (usedSlot != null)
		{
			usedSlot.OnSlotContentsChangeServer.RemoveListener(OnSlotContentsChanged);
		}
		playerScript.playerMove.OnCuffChangeServer.RemoveListener(OnCuffChange);
		playerScript.registerTile.OnSlipChangeServer.RemoveListener(OnSlipChange);
		playerScript.playerHealth.OnConsciousStateChangeServer.RemoveListener(OnConsciousStateChange);
		playerScript.registerTile.OnLocalPositionChangedServer.RemoveListener(OnLocalPositionChanged);
		playerScript.playerDirectional.OnDirectionChange.RemoveListener(OnDirectionChanged);
		if (crossMatrix)
		{
			if (startProgressInfo.Target.TargetMatrixInfo.MatrixMove != null)
			{
				startProgressInfo.Target.TargetMatrixInfo.MatrixMove.MatrixMoveEvents.OnStartMovementServer.RemoveListener(OnMatrixStartMove);
				startProgressInfo.Target.TargetMatrixInfo.MatrixMove.MatrixMoveEvents.OnRotate.RemoveListener(OnTargetMatrixRotate);
			}

			var performerMatrix = playerScript.registerTile.Matrix;
			if (performerMatrix.MatrixMove != null)
			{
				startProgressInfo.Target.TargetMatrixInfo.MatrixMove.MatrixMoveEvents.OnStartMovementServer.RemoveListener(OnMatrixStartMove);
				startProgressInfo.Target.TargetMatrixInfo.MatrixMove.MatrixMoveEvents.OnRotate.RemoveListener(OnTargetMatrixRotate);
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
		if (progressActionConfig.InterruptsOverlapping)
		{
			//interrupt other progress bars of the same action type on the same location
			var existingBars = UIManager.Instance.ProgressBars
				.Where(pb => pb != ProgressBar && pb.ServerProgressAction is StandardProgressAction)
				.Where(pb =>
					((StandardProgressAction) pb.ServerProgressAction).progressActionConfig.StandardProgressActionType == progressActionConfig.StandardProgressActionType)
				.Where(pb => pb.transform.parent == ProgressBar.transform.parent)
				.Where(pb => Vector3.Distance(pb.transform.localPosition, startProgressInfo.Target.TargetLocalPosition) < 0.1)
				.ToList();

			foreach (var existingBar in existingBars)
			{
				Logger.LogTraceFormat("Server interrupting progress bar {0} because progress bar {1} finished " +
				                      "on same tile", Category.ProgressAction, existingBar.ID, ProgressBar.ID);
				existingBar.ServerInterruptProgress();
			}
		}

		UnregisterHooks();
		if (info.WasCompleted)
		{
			onCompletion?.Invoke();
		}
	}

	private void InterruptProgress(string reason)
	{
		Logger.LogTraceFormat("Server progress bar {0} interrupted: {1}.", Category.ProgressAction,
			ProgressBar.ID, reason);
		ProgressBar.ServerInterruptProgress();
	}

	private void OnTargetMatrixRotate(MatrixRotationInfo arg0)
	{
		InterruptProgress("target matrix rotated");
	}

	private bool CanPlayerStillProgress()
	{
		//note: doesn't check cross matrix situations.
		return playerScript.playerHealth.ConsciousState == initialConsciousState &&
		       !playerScript.playerMove.IsCuffed &&
		       !playerScript.registerTile.IsSlippingServer &&
		       (progressActionConfig.AllowTurning ||
		        playerScript.playerDirectional.CurrentDirection != initialDirection) &&
		       !playerScript.PlayerSync.IsMoving &&
		       //make sure we're still in range
		       Validations.IsInReach(playerScript.registerTile.WorldPositionServer,
			       startProgressInfo.Target.TargetWorldPosition);
			;
	}

	private void OnWelderOff()
	{
		InterruptProgress("welder off");
	}

	private void OnSlotContentsChanged()
	{
		InterruptProgress("performer's active slot contents changed");
	}

	private void OnMatrixStartMove()
	{
		InterruptProgress("cross-matrix and performer or target matrix started moving");
	}

	private void OnLocalPositionChanged(Vector3Int arg0)
	{
		//if player or target moves at all, interrupt
		InterruptProgress("performer or target moved");
	}

	private void OnConsciousStateChange(ConsciousState oldState, ConsciousState newState)
	{
		if (!CanPlayerStillProgress()) InterruptProgress("performer not conscious enough");
	}

	private void OnSlipChange(bool wasSlipped, bool nowSlipped)
	{
		if (!CanPlayerStillProgress()) InterruptProgress("performer slipped");
	}

	private void OnCuffChange(bool wasCuffed, bool nowCuffed)
	{
		if (!CanPlayerStillProgress()) InterruptProgress("performer cuffed");
	}


	private void OnDirectionChanged(Orientation arg0)
	{
		if (!CanPlayerStillProgress()) InterruptProgress("performer direction changed");
	}
}
