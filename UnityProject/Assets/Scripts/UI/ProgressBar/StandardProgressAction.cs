
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
				//already doing this action, and multiple is not allowed
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
			return false;
		}

		//is this cross matrix? if so, don't start progress if matrix is moving
		var performerMatrix = playerScript.registerTile.Matrix;
		crossMatrix = performerMatrix != info.Target.TargetMatrixInfo.Matrix;
		if (crossMatrix && info.Target.TargetMatrixInfo.MatrixMove != null &&
		    info.Target.TargetMatrixInfo.MatrixMove.IsMovingServer)
		{
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
			welder.OnWelderOffServer.AddListener(InterruptProgress);
		}
		//if targeting an object, interrupt if object moves away
		if (startProgressInfo.Target.IsObject)
		{
			startProgressInfo.Target.Target.OnLocalPositionChangedServer.AddListener(OnLocalPositionChanged);
		}
		//interrupt if active hand slot changes
		var activeSlot = playerScript.ItemStorage.GetActiveHandSlot();
		activeSlot.OnSlotContentsChangeServer.AddListener(InterruptProgress);
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
		//interrupt if tile is on different matrix and moves / rotates away from player
		if (crossMatrix && startProgressInfo.Target.TargetMatrixInfo.MatrixMove != null)
		{
			startProgressInfo.Target.TargetMatrixInfo.MatrixMove.MatrixMoveEvents.OnStartMovementServer.AddListener(InterruptProgress);
			startProgressInfo.Target.TargetMatrixInfo.MatrixMove.MatrixMoveEvents.OnRotate.AddListener(OnTargetMatrixRotate);
		}
	}

	private void UnregisterHooks()
	{
		if (welder)
		{
			welder.OnWelderOffServer.RemoveListener(InterruptProgress);
		}
		if (startProgressInfo.Target.IsObject)
		{
			startProgressInfo.Target.Target.OnLocalPositionChangedServer.RemoveListener(OnLocalPositionChanged);
		}
		var activeSlot = playerScript.ItemStorage.GetActiveHandSlot();
		if (usedSlot != null)
		{
			activeSlot.OnSlotContentsChangeServer.RemoveListener(InterruptProgress);
		}
		playerScript.playerMove.OnCuffChangeServer.RemoveListener(OnCuffChange);
		playerScript.registerTile.OnSlipChangeServer.RemoveListener(OnSlipChange);
		playerScript.playerHealth.OnConsciousStateChangeServer.RemoveListener(OnConsciousStateChange);
		playerScript.PlayerSync.OnTileReached().RemoveListener(OnLocalPositionChanged);
		playerScript.playerDirectional.OnDirectionChange.RemoveListener(OnDirectionChanged);
		if (crossMatrix && startProgressInfo.Target.TargetMatrixInfo.MatrixMove != null)
		{
			startProgressInfo.Target.TargetMatrixInfo.MatrixMove.MatrixMoveEvents.OnStartMovementServer.RemoveListener(InterruptProgress);
			startProgressInfo.Target.TargetMatrixInfo.MatrixMove.MatrixMoveEvents.OnRotate.RemoveListener(OnTargetMatrixRotate);
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
				.Where(pb => pb != startProgressInfo.ProgressBar && pb.ServerProgressAction is StandardProgressAction)
				.Where(pb =>
					((StandardProgressAction) pb.ServerProgressAction).progressActionConfig.StandardProgressActionType == progressActionConfig.StandardProgressActionType)
				.Where(pb => pb.transform.parent == startProgressInfo.ProgressBar.transform.parent)
				.Where(pb => Vector3.Distance(pb.transform.localPosition, startProgressInfo.Target.TargetLocalPosition) < 0.1)
				.ToList();

			foreach (var existingBar in existingBars)
			{
				existingBar.ServerInterruptProgress();
			}
		}

		UnregisterHooks();
		if (info.WasCompleted)
		{
			onCompletion?.Invoke();
		}
	}

	private void InterruptProgress()
	{
		startProgressInfo.ProgressBar.ServerInterruptProgress();
	}

	private void OnTargetMatrixRotate(MatrixRotationInfo arg0)
	{
		InterruptProgress();
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

	private void OnLocalPositionChanged(Vector3Int arg0)
	{
		//if player or target moves at all, interrupt
		InterruptProgress();
	}

	private void OnConsciousStateChange(ConsciousState oldState, ConsciousState newState)
	{
		if (!CanPlayerStillProgress()) InterruptProgress();
	}

	private void OnSlipChange(bool wasSlipped, bool nowSlipped)
	{
		if (!CanPlayerStillProgress()) InterruptProgress();
	}

	private void OnCuffChange(bool wasCuffed, bool nowCuffed)
	{
		if (!CanPlayerStillProgress()) InterruptProgress();
	}


	private void OnDirectionChanged(Orientation arg0)
	{
		if (!CanPlayerStillProgress()) InterruptProgress();
	}



}
