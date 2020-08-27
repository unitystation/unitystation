using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Mirror;
using Random = UnityEngine.Random;

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

	private IProgressAction progressAction;
	/// <summary>
	/// Progress action this bar is displaying. Valid server side only.
	/// </summary>
	public IProgressAction ServerProgressAction => progressAction;
	/// <summary>
	/// registerPlayer of player who initiated it
	/// </summary>
	public RegisterPlayer RegisterPlayer => registerPlayer;

	//playerSync, move, and health of player who initiated it

	private float progUnit { get { return timeToFinish / 21f; } }
	private int spriteIndex { get { return Mathf.Clamp((int) (progress / progUnit), 0, 20); } }
	private int lastSpriteIndex = 0;
	private bool timeToNotifyPlayer { get { return lastSpriteIndex != spriteIndex; } }

	//matrix move the progress bar is on, null if none.
	private MatrixMove matrixMove;

	private void Awake()
	{
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		//always upright in world space
		transform.rotation = Quaternion.identity;
	}

	/// <summary>
	/// For progress action system internal use only. Please use ProgressAction.ServerStartProgress to initiate a progress action
	/// on the server side.
	///
	/// Initiate this progress bar's behavior on server side. Assumes position is already set to where the progress
	/// bar should appear.
	/// </summary>
	/// <param name="progressAction">progress action being performed</param>
	/// <param name="startInfo">info on the started action</param>
	public void _ServerStartProgress(IProgressAction progressAction, StartProgressInfo startInfo)
	{
		done = true;
		progress = 0f;
		lastSpriteIndex = 0;
		timeToFinish = startInfo.TimeForCompletion;
		registerPlayer = startInfo.Performer.GetComponent<RegisterPlayer>();
		this.progressAction = progressAction;
		id = GetInstanceID();

		if (startInfo.Performer != PlayerManager.LocalPlayer)
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
		ProgressBarMessage.SendCreate(startInfo.Performer, 0, (transform.position - startInfo.Performer.transform.position).To2Int(), id);
	}

	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
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

	/// <summary>
	/// Logic for starting a progress bar used on both client and server.
	/// </summary>
	private void CommonStartProgress()
	{
		//always upright in world space
		transform.rotation = Quaternion.identity;
		done = false;
		//common logic used between client / server progress start logic
		matrixMove = GetComponentInParent<MatrixMove>();
		if (matrixMove != null)
		{
			matrixMove.MatrixMoveEvents.OnRotate.AddListener(OnRotationEnd);
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

	private void OnRotationEnd(MatrixRotationInfo info)
	{
		if (info.IsClientside && info.IsEnding)
		{
			//reset orientation to upright
			transform.rotation = Quaternion.identity;
		}
	}

	private void DestroyProgressBar()
	{
		done = true;
		spriteRenderer.transform.parent.localRotation = Quaternion.identity;
		spriteRenderer.enabled = false;

		if (matrixMove != null)
		{
			matrixMove.MatrixMoveEvents.OnRotate.RemoveListener(OnRotationEnd);
		}
		UIManager.DestroyProgressBar(id);
	}

	public void ClientUpdateProgress(int newSpriteIndex)
	{
		lastSpriteIndex = newSpriteIndex;
		// -1 sent from server means the crafting is complete. dismiss the progress bar:
		if (newSpriteIndex == -1)
		{
			Logger.LogTraceFormat("Client stopping progress bar {0} because server told us it's done", Category.ProgressAction, ID);
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

	void UpdateMe()
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

		//check if progress should continue
		if (!progressAction.OnServerContinueProgress(new InProgressInfo(progress)))
		{
			// Remove from UpdateMe before invoking action, lest action fails and so infinite loop.
			ServerCloseProgressBar();
			progressAction.OnServerEndProgress(new EndProgressInfo(false));
			Logger.LogTraceFormat("Server progress bar {0} interrupted.", Category.ProgressAction, ID);
		}

		//Finished! Invoke the action and close the progress bar for the player
		if (progress >= timeToFinish)
		{
			// Remove from UpdateMe before invoking action, lest action fails and so infinite loop.
			ServerCloseProgressBar();
			progressAction.OnServerEndProgress(new EndProgressInfo(true));
			Logger.LogTraceFormat("Server progress bar {0} completed.", Category.ProgressAction, ID);
		}
	}

	/// <summary>
	/// Interrupt the progress bar, closing it prematurely
	/// </summary>
	/// <param name="progressEndReason">reason progress was interrupted</param>
	public void ServerInterruptProgress()
	{
		//already closed?
		if (done || progressAction == null) return;

		ServerCloseProgressBar();
		progressAction.OnServerEndProgress(new EndProgressInfo(false));
		Logger.LogTraceFormat("Server progress bar {0} interrupted.", Category.ProgressAction, ID);
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
			Logger.LogTraceFormat("Server telling {0} to close progress bar {1}", Category.ProgressAction, registerPlayer.gameObject, ID);
			ProgressBarMessage.SendUpdate(registerPlayer.gameObject, COMPLETE_INDEX, id);

			//destroy server's local copy
			DestroyProgressBar();
		}
	}
}
