using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

//Server controls the progress rate and finishedAction is used serverside to determine the action when progress completes
//As there may be many progress bars being used by multiple players throughout then the server needs to
//be capable of keeping track of more then 1 progress state at any given time
public class ProgressBar : NetworkBehaviour
{
	public Sprite[] progressSprites;

	private SpriteRenderer spriteRenderer;

	//only useful serverside:
	private List<PlayerProgressEntry> playerProgress = new List<PlayerProgressEntry>();

	void Start()
	{
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		spriteRenderer.gameObject.SetActive(false);
		UIManager.Instance.progressBar = this;
	}

	///<Summary>
	/// To use send the worldpos, time it should take to complete, the
	/// ServerSide method that should be called on completion and the
	/// Player the progress bar is for.
	///</Summary>

	[Server]
	public void StartProgress(Vector3 pos, float timeForCompletion,
		FinishProgressAction finishProgressAction, GameObject _player,
		string _additionalSfx = "", float _additionalSfxPitch = 1f, bool _allowTurning = true)
	{
		var _playerDirectional = _player.GetComponent<Directional>();
		playerProgress.Add(new PlayerProgressEntry
		{
			player = _player,
				timeToFinish = timeForCompletion,
				completedAction = finishProgressAction,
				position = pos,
				playerDirectional = _playerDirectional,
				playerPositionCache = _player.TileWorldPosition(),
				facingDirectionCache = _playerDirectional.CurrentDirection,
				additionalSfx = _additionalSfx,
				additionalSfxPitch = _additionalSfxPitch,
				allowTurning = _allowTurning
		});

		//Start the progress for the player:
		ProgressBarMessage.Send(_player, 0, pos);
	}

	public void ClientUpdateProgress(Vector3 pos, int spriteIndex)
	{
		if (!spriteRenderer.gameObject.activeInHierarchy)
		{
			spriteRenderer.gameObject.SetActive(true);
		}

		// -1 sent from server means the crafting is complete. dismiss the progress bar:
		if (spriteIndex == -1)
		{
			spriteRenderer.gameObject.SetActive(false);
			return;
		}

		transform.position = pos;
		spriteRenderer.sprite = progressSprites[spriteIndex];
	}

	void Update()
	{
		if (playerProgress.Count > 0)
		{
			UpdateProgressBars();
		}
	}

	//Server only:
	private void UpdateProgressBars()
	{
		for (int i = playerProgress.Count - 1; i >= 0; i--)
		{
			playerProgress[i].progress += Time.deltaTime;
			if (playerProgress[i].timeToNotifyPlayer)
			{
				//Update the players progress bar
				ProgressBarMessage.Send(playerProgress[i].player,
					playerProgress[i].spriteIndex, playerProgress[i].position);

				if(playerProgress[i].spriteIndex == 12){
					//Almost done, check to see if there is an additionalSFX to play:
					playerProgress[i].PlayAdditionalSound();
				}
			}

			//Cancel the progress bar if the player moves away or faces another direction:
			if (playerProgress[i].HasMovedAway())
			{
				playerProgress[i].completedAction.Finish(FinishProgressAction.FinishReason.INTERRUPTED);
				CloseProgressBar(playerProgress[i]);
				continue;
			}

			//Finished! Invoke the action and close the progress bar for the player
			if (playerProgress[i].progress >= playerProgress[i].timeToFinish)
			{
				playerProgress[i].completedAction.Finish(FinishProgressAction.FinishReason.COMPLETED);
				CloseProgressBar(playerProgress[i]);
			}
		}
	}

	private void CloseProgressBar(PlayerProgressEntry playerProg)
	{
		//Notify player to turn off progress bar:
		ProgressBarMessage.Send(playerProg.player, -1, playerProg.position);
		//remove from the player progress list:
		playerProgress.Remove(playerProg);
	}
}

public class PlayerProgressEntry
{
	public float progress = 0f;
	public float timeToFinish;
	public GameObject player;
	public Directional playerDirectional;
	public Vector2Int playerPositionCache;
	public Orientation facingDirectionCache;
	public FinishProgressAction completedAction;
	public Vector3 position;
	public bool allowTurning;
	public float progUnit { get { return timeToFinish / 21f; } }
	public int spriteIndex { get { return Mathf.Clamp((int) (progress / progUnit), 0, 20); } }
	public int lastSpriteIndex = 0;
	public bool timeToNotifyPlayer { get { return lastSpriteIndex != spriteIndex; } }
	public string additionalSfx = "";  // leave empty if you don't want one to play (plays at sprite index 12)
	public float additionalSfxPitch = 1f;

	//has the player moved away while the progress bar is in progress?
	public bool HasMovedAway()
	{
		if ((!allowTurning && playerDirectional.CurrentDirection != facingDirectionCache) ||
			player.TileWorldPosition() != playerPositionCache)
		{
			return true;
		}
		return false;
	}

	public void PlayAdditionalSound()
	{
		if (!string.IsNullOrEmpty(additionalSfx))
		{
			SoundManager.PlayNetworkedAtPos(additionalSfx, position, additionalSfxPitch);
		}
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