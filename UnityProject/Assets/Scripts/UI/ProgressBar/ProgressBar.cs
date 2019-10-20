using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Mirror;

/// <summary>
/// Main behavior for progress bars. Progress bars progress is tracked on the server and the client
/// that initiated the action only gets sprite index updates. Other players do not receive any updates.
///
/// Due to the pecularities of how it only needs to appear for one player, this doesn't use monobehavior /
/// registertile / CNT...it is just a regular game object and is updated in response to net messages.
///
/// NOTE: Might want to turn this into a more re-usable system if there are other things that should have
/// this sort of behavior - known only to one client and the server but still being able to use
/// registerTile.
/// </summary>
public class ProgressBar : MonoBehaviour
{
	private static readonly int COMPLETE_INDEX = -1;


	private SpriteRenderer spriteRenderer;
	public Sprite[] progressSprites;
	//unique id of this progress bar - unique across all players
	private int id;
	/// <summary>
	/// Unique id of this progress bar.
	/// </summary>
	public int ID => id;


	//---all of the below fields are valid only on the server---

	//how much time progress has been going on for
	private float progress = 0f;
	//total duration until this progress bar should be complete
	private float timeToFinish;
	private bool done;
	//registerPlayer of player who initiated it
	private RegisterPlayer registerPlayer;

	/// <summary>
	/// registerPlayer of player who initiated it
	/// </summary>
	public RegisterPlayer RegisterPlayer => registerPlayer;

	//playerSync of player who initiated it
	private PlayerSync playerSync;
	//directional of the player who initiated it
	private Directional playerDirectional;
	//pickupable item associated with the action (if any)
	//progress will be cancelled on drop
	private Pickupable usedItem;
	//initial orientation of player when they initiated it
	private Orientation facingDirectionCache;
	//Action which should be invoked when progress is done (for one reason or another)
	private FinishProgressAction completedAction;
	//whether player turning is allowed during progress
	private bool allowTurning;
	private float progUnit { get { return timeToFinish / 21f; } }
	private int spriteIndex { get { return Mathf.Clamp((int) (progress / progUnit), 0, 20); } }
	private int lastSpriteIndex = 0;
	private bool timeToNotifyPlayer { get { return lastSpriteIndex != spriteIndex; } }
	//matrix move the progress bar is on, null if none.
	private MatrixMove matrixMove;

	private void Awake()
	{
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
	}

	/// <summary>
	/// Initiate this progress bar's behavior on server side. Assumes position is already set to where the progress
	/// bar should appear.
	/// </summary>
	/// <param name="timeForCompletion">how long in seconds the action should take</param>
	/// <param name="finishProgressAction">callback for when action completes or is interrupted</param>
	/// <param name="player">player performing the action</param>
	/// <param name="usingHandItem">true if interaction is being performed using item in active hand.
	/// If this item is dropped or swapped to different slot or active slot is changed, progress will be interrupted.</param>
	/// <param name="allowTurning">if true (default), turning won't interrupt progress</param>
	public void ServerStartProgress(float timeForCompletion,
		FinishProgressAction finishProgressAction, GameObject player, bool usingHandItem = false, bool allowTurning = true)
	{
		done = true;
		playerDirectional = player.GetComponent<Directional>();
		progress = 0f;
		lastSpriteIndex = 0;
		timeToFinish = timeForCompletion;
		completedAction = finishProgressAction;
		facingDirectionCache = playerDirectional.CurrentDirection;
		registerPlayer = player.GetComponent<RegisterPlayer>();
		playerSync = player.GetComponent<PlayerSync>();
		this.allowTurning = allowTurning;
		id = GetInstanceID();

		if (usingHandItem)
		{
			var usedItemObj = player.Player().Script.playerNetworkActions.GetActiveHandItem();
			if (usedItemObj == null)
			{
				//nothing in hand, do not process any further
				Logger.LogWarningFormat("For player {0}, usingHandItem=true but no item in hand. Progress action will not proceed", Category.UI, player.name);
				return;
			}

			usedItem  = usedItemObj.GetComponent<Pickupable>();
			if (usedItem == null)
			{
				//item in hand not pickupable, do not process further
				Logger.LogWarningFormat("For player {0}, usingHandItem=true but {1} is not pickupable. Progress action will not proceed", Category.UI, player.name, usedItemObj.name);
				return;
			}
		}


		if (player != PlayerManager.LocalPlayer)
		{
			//server should not see clients progress bar
			spriteRenderer.enabled = false;
		}
		else
		{
			spriteRenderer.enabled = true;
		}
		spriteRenderer.sprite = progressSprites[0];


		if (this.usedItem != null)
		{
			this.usedItem.OnDropServer.AddListener(ServerInterruptOnDrop);
		}

		matrixMove = GetComponentInParent<MatrixMove>();
		if (matrixMove != null)
		{
			matrixMove.OnRotateEnd.AddListener(OnRotationEnd);
		}

		done = false;

		//Start the progress for the player:
		//note: using transform position for the offset, because progress bar has no register tile and
		//otherwise it would give an incorrect offset if player is on moving matrix
		ProgressBarMessage.SendCreate(player, 0, (transform.position - player.transform.position).To2Int(), id);


	}

	private void OnRotationEnd(RotationOffset arg0, bool arg1)
	{
		//reset orientation to upright
		transform.rotation = Quaternion.identity;
	}

	private void ServerInterruptOnDrop()
	{
		//called when item is dropped, interrupts progress
		ServerInterruptProgress();
	}

	/// <summary>
	/// Invoke when bar is created client side, assigns the id
	/// </summary>
	/// <param name="progressBarId">id to assign to this bar</param>
	/// <param name="offsetFromPlayer">offset of bar from player, to ensure positioning is correct</param>
	public void ClientStartProgress(int progressBarId, Vector2Int offsetFromPlayer)
	{
		id = progressBarId;
		matrixMove = GetComponentInParent<MatrixMove>();
		if (matrixMove != null)
		{
			matrixMove.OnRotateEnd.AddListener(OnRotationEnd);
		}

	}

	private void DestroyProgressBar()
	{
		done = true;
		spriteRenderer.enabled = false;
		if (this.usedItem != null)
		{
			this.usedItem.OnDropServer.RemoveListener(ServerInterruptOnDrop);
		}
		if (matrixMove != null)
		{
			matrixMove.OnRotateEnd.RemoveListener(OnRotationEnd);
		}
		UIManager.DestroyProgressBar(id);
	}

	public void ClientUpdateProgress(int newSpriteIndex)
	{
		// -1 sent from server means the crafting is complete. dismiss the progress bar:
		if (newSpriteIndex == -1)
		{
			DestroyProgressBar();
			return;
		}

		if (registerPlayer != null && registerPlayer.gameObject != PlayerManager.LocalPlayer)
		{
			//this is for server's copy of client's progress bar -
			//server should not render clients progress bar
			return;
		}

		if (!spriteRenderer.enabled)
		{
			spriteRenderer.enabled = true;
		}

		spriteRenderer.sprite = progressSprites[newSpriteIndex];
	}

	void Update()
	{
		//server only
		if (registerPlayer == null || done) return;

		progress += Time.deltaTime;
		if (timeToNotifyPlayer)
		{
			//Update the players progress bar
			ProgressBarMessage.SendUpdate(registerPlayer.gameObject, spriteIndex, id);
			lastSpriteIndex = spriteIndex;
		}

		//Cancel the progress bar if the player moves away or faces another direction:
		if (Interrupted())
		{
			completedAction.Finish(FinishReason.INTERRUPTED);
			ServerCloseProgressBar();
			return;
		}

		//Finished! Invoke the action and close the progress bar for the player
		if (progress >= timeToFinish)
		{
			completedAction.Finish(FinishReason.COMPLETED);
			ServerCloseProgressBar();
		}
	}

	//has the player moved away while the progress bar is in progress?
	private bool Interrupted()
	{
		return TurnInterrupt() ||
		       PlayerMoved() ||
		       TargetMovedAway();
	}

	private bool TargetMovedAway()
	{
		//NOTE: using transform position for this check because otherwise it would
		//return invalid distance when matrix rotates
		return (transform.position - registerPlayer.transform.position).magnitude > 1.5f;
	}

	private bool PlayerMoved()
	{
		return playerSync.IsMoving;
	}

	private bool TurnInterrupt()
	{
		return !allowTurning && playerDirectional.CurrentDirection != facingDirectionCache;
	}

	/// <summary>
	/// Interrupt the progress bar, closing it prematurely
	/// </summary>
	/// <param name="finishReason">reason progress was interrupted</param>
	public void ServerInterruptProgress(FinishReason finishReason = FinishReason.INTERRUPTED)
	{
		completedAction.Finish(finishReason);
		ServerCloseProgressBar();
	}


	private void ServerCloseProgressBar()
	{
		//Notify player to turn off progress bar:
		ProgressBarMessage.SendUpdate(registerPlayer.gameObject, COMPLETE_INDEX, id);
		if (PlayerManager.LocalPlayer != registerPlayer.gameObject)
		{
			//destroy server's own copy of the bar
			DestroyProgressBar();
		}


	}
}