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

	private float anim;
	private int animIdx;
	private float animSpd;
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

	private ProgressAction progressAction;
	/// <summary>
	/// registerPlayer of player who initiated it
	/// </summary>
	public RegisterPlayer RegisterPlayer => registerPlayer;

	//playerSync of player who initiated it
	private PlayerSync playerSync;
	//directional of the player who initiated it
	private Directional playerDirectional;
	//slot being used to perform the action, will be interrupted if slot contents change
	private ItemSlot usedSlot;
	//initial orientation of player when they initiated it
	private Orientation facingDirectionCache;
	//Action which should be invoked when progress is done (for one reason or another)
	private IProgressEndAction completedEndAction;
	private float progUnit { get { return timeToFinish / 21f; } }
	private int spriteIndex { get { return Mathf.Clamp((int) (progress / progUnit), 0, 20); } }
	private int lastSpriteIndex = 0;
	private bool timeToNotifyPlayer { get { return lastSpriteIndex != spriteIndex; } }

	public ProgressAction ProgressAction => progressAction;

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
	/// <param name="progressAction">progress action being performed</param>
	/// <param name="timeForCompletion">how long in seconds the action should take</param>
	/// <param name="progressEndAction">callback for when action completes or is interrupted</param>
	/// <param name="player">player performing the action</param>
	public void ServerStartProgress(ProgressAction progressAction, float timeForCompletion,
		IProgressEndAction progressEndAction, GameObject player)
	{
		done = true;
		playerDirectional = player.GetComponent<Directional>();
		progress = 0f;
		lastSpriteIndex = 0;
		timeToFinish = timeForCompletion;
		completedEndAction = progressEndAction;
		facingDirectionCache = playerDirectional.CurrentDirection;
		registerPlayer = player.GetComponent<RegisterPlayer>();
		playerSync = player.GetComponent<PlayerSync>();
		this.progressAction = progressAction;
		id = GetInstanceID();

		//interrupt if hand contents are changed
		var activeSlot = player.Player().Script.ItemStorage.GetActiveHandSlot();
		activeSlot.OnSlotContentsChangeServer.AddListener(ServerInterruptOnInvChange);
		this.usedSlot = activeSlot;


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

		CommonStartProgress();

		//Start the progress for the player:
		//note: using transform position for the offset, because progress bar has no register tile and
		//otherwise it would give an incorrect offset if player is on moving matrix
		ProgressBarMessage.SendCreate(player, 0, (transform.position - player.transform.position).To2Int(), id);
	}

	private void ServerInterruptOnInvChange()
	{
		//called when active hand slot is changed, interrupts progress
		ServerInterruptProgress();
	}

	/// <summary>
	/// Invoke when bar is created client side, assigns the id
	/// </summary>
	/// <param name="progressBarId">id to assign to this bar</param>
	public void ClientStartProgress(int progressBarId)
	{
		id = progressBarId;
		CommonStartProgress();

	}

	private void CommonStartProgress()
	{
		done = false;
		//common logic used between client / server progress start logic
		matrixMove = GetComponentInParent<MatrixMove>();
		if (matrixMove != null)
		{
			matrixMove.OnRotateEnd.AddListener(OnRotationEnd);
		}

		anim = 0f;
		if (Random.value < 0.02f)
		{
			spriteRenderer.transform.parent.localRotation = Quaternion.identity;
			animIdx = Random.Range(1, progressSprites.Length / 2);
			animSpd = Random.Range(360f, 720f);
		}
		else
		{
			animIdx = -1;
		}
	}

	private void OnRotationEnd(RotationOffset arg0, bool arg1)
	{
		//reset orientation to upright
		transform.rotation = Quaternion.identity;
	}



	private void DestroyProgressBar()
	{
		done = true;
		spriteRenderer.transform.parent.localRotation = Quaternion.identity;
		spriteRenderer.enabled = false;
		usedSlot?.OnSlotContentsChangeServer.RemoveListener(ServerInterruptOnInvChange);



		if (matrixMove != null)
		{
			matrixMove.OnRotateEnd.RemoveListener(OnRotationEnd);
		}
		UIManager.DestroyProgressBar(id);
	}

	public void ClientUpdateProgress(int newSpriteIndex)
	{
		lastSpriteIndex = newSpriteIndex;
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
		if (done) return;
		if (animIdx != -1 && lastSpriteIndex >= animIdx)
		{
			anim += Time.deltaTime * animSpd;
			var deg = Mathf.Lerp(0, 360, Mathf.SmoothStep(0f, 1f, Mathf.SmoothStep(0f, 1f, anim / 360f)));
			spriteRenderer.transform.parent.localRotation = Quaternion.Euler(0, 0, deg);
			if (deg >= 360)
			{
				spriteRenderer.transform.parent.localRotation = Quaternion.identity;
				animIdx = -1;
			}
		}
		//server only
		if (registerPlayer == null) return;

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
			completedEndAction.OnEnd(ProgressEndReason.INTERRUPTED);
			ServerCloseProgressBar();
			return;
		}

		//Finished! Invoke the action and close the progress bar for the player
		if (progress >= timeToFinish)
		{
			completedEndAction.OnEnd(ProgressEndReason.COMPLETED);
			if (progressAction.InterruptsOverlapping)
			{
				//interrupt all other progress actions of this type at this location
				UIManager.ServerInterruptProgress(this, progressAction, transform.localPosition, transform.parent);

			}
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
		return !progressAction.AllowTurning && playerDirectional.CurrentDirection != facingDirectionCache;
	}

	/// <summary>
	/// Interrupt the progress bar, closing it prematurely
	/// </summary>
	/// <param name="progressEndReason">reason progress was interrupted</param>
	public void ServerInterruptProgress(ProgressEndReason progressEndReason = ProgressEndReason.INTERRUPTED)
	{
		completedEndAction.OnEnd(progressEndReason);
		ServerCloseProgressBar();
	}


	private void ServerCloseProgressBar()
	{
		done = true;
		//Notify player to turn off progress bar:
		if (PlayerManager.LocalPlayer == registerPlayer.gameObject)
		{
			//server player's bar, just destroy it
			DestroyProgressBar();
		}
		else
		{
			//inform client
			ProgressBarMessage.SendUpdate(registerPlayer.gameObject, COMPLETE_INDEX, id);
			//destroy server's local copy
			DestroyProgressBar();
		}
	}
}