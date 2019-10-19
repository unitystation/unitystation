using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Mirror;

/// <summary>
/// Main behavior for progress bars. Progress bars progress is tracked on the server and the client
/// that initiated the action only gets sprite index updates. Other players do not receive any updates.
/// </summary>
public class ProgressBar : MonoBehaviour
{
	private static readonly int COMPLETE_INDEX = -1;

	public Sprite[] progressSprites;
	//unique id of this progress bar - unique across all players
	private int id;
	/// <summary>
	/// Unique id of this progress bar.
	/// </summary>
	public int ID => id;

	//all of the below fields are valid only on the server
	//how much time progress has been going on for
	private float progress = 0f;
	//total duration until this progress bar should be complete
	private float timeToFinish;
	private bool done;
	//player who initiated it
	private GameObject player;
	//directional of the player who initiated it
	private Directional playerDirectional;
	//position at which the player initiated it
	private Vector2Int playerWorldPosCache;
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

	private SpriteRenderer spriteRenderer;

	private void Awake()
	{
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
	}

	//has the player moved away while the progress bar is in progress?
	private bool HasMovedAway()
	{
		//TODO: This won't work correctly on moving or rotating matrices. Instead, manually interrupt progress when player
		//movement or turning happens.

		if ((!allowTurning && playerDirectional.CurrentDirection != facingDirectionCache) ||
		    player.TileWorldPosition() != playerWorldPosCache)
		{
			return true;
		}
		return false;
	}


	/// <summary>
	/// Initiate this progress bar's behavior on server side. Assumes position is already set to where the progress
	/// bar should appear.
	/// </summary>
	/// <param name="timeForCompletion">how long in seconds the action should take</param>
	/// <param name="finishProgressAction">callback for when action completes or is interrupted</param>
	/// <param name="player">player performing the action</param>
	/// <param name="allowTurning">if true (default), turning won't interrupt progress</param>
	public void ServerStartProgress(float timeForCompletion,
		FinishProgressAction finishProgressAction, GameObject player, bool allowTurning = true)
	{
		playerDirectional = player.GetComponent<Directional>();
		progress = 0f;
		lastSpriteIndex = 0;
		timeToFinish = timeForCompletion;
		completedAction = finishProgressAction;
		playerWorldPosCache = player.TileWorldPosition();
		facingDirectionCache = playerDirectional.CurrentDirection;
		this.player = player;
		this.allowTurning = allowTurning;
		id = GetInstanceID();

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



		done = false;

		//Start the progress for the player:
		//okay to use transform.position here since it's just been spawned
		ProgressBarMessage.SendCreate(player, 0, transform.position.To2Int() - playerWorldPosCache, id);
	}

	/// <summary>
	/// Invoke when bar is created client side, assigns the id
	/// </summary>
	/// <param name="progressBarId">id to assign to this bar</param>
	public void ClientStartProgress(int progressBarId)
	{
		id = progressBarId;
	}

	public void ClientUpdateProgress(int newSpriteIndex)
	{

		// -1 sent from server means the crafting is complete. dismiss the progress bar:
		if (newSpriteIndex == -1)
		{
			spriteRenderer.enabled = false;
			UIManager.DestroyProgressBar(id);
			return;
		}

		if (player != null && player != PlayerManager.LocalPlayer)
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
		if (player == null || done) return;

		progress += Time.deltaTime;
		if (timeToNotifyPlayer)
		{
			//Update the players progress bar
			ProgressBarMessage.SendUpdate(player, spriteIndex, id);
		}

		//Cancel the progress bar if the player moves away or faces another direction:
		if (HasMovedAway())
		{
			completedAction.Finish(FinishProgressAction.FinishReason.INTERRUPTED);
			ServerCloseProgressBar();
			return;
		}

		//Finished! Invoke the action and close the progress bar for the player
		if (progress >= timeToFinish)
		{
			completedAction.Finish(FinishProgressAction.FinishReason.COMPLETED);
			ServerCloseProgressBar();
		}
	}

	private void ServerCloseProgressBar()
	{
		done = true;
		//Notify player to turn off progress bar:
		ProgressBarMessage.SendUpdate(player, COMPLETE_INDEX, id);
	}
}

/// <summary>
/// Defines what to do when finishing progress bar, which could be due to the progress completing
/// or being interrupted. Pretty sure this runs only on the server.
/// </summary>
public class FinishProgressAction
{
	/// <summary>
	/// Denotes why the action in progress is now done
	/// </summary>
	public enum FinishReason
	{
		//completed successfully
		COMPLETED,
		//interrupted before completion
		INTERRUPTED
		//Add whatever else you need here
	}

	//callback invoked when action completes
	private Action<FinishReason> onFinished;

	/// <summary>
	/// Finish progress action with a specified callback when finished
	/// </summary>
	/// <param name="onFinished">function to invoke when progress is finished, including an indicator
	/// of why the progress finished (such as if it was interrupted). The function should
	/// take care of whatever needs to be done based on FinishStatus status.</param>
	public FinishProgressAction(Action<FinishReason> onFinished)
	{
		this.onFinished = onFinished;
	}

	/// <summary>
	/// Finish the action with the specified reason, invoke the callback.
	/// </summary>
	/// <param name="completed">reason for completion</param>
	public void Finish(FinishReason reason)
	{
		this.onFinished.Invoke(reason);
	}
}