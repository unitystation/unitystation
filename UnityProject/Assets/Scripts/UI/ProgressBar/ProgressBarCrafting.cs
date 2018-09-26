using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

//Server controls the progress rate and finishedAction is called serverside to finish the crafting
//As there may be many progress bars being used by multiple players throughout then the server needs to
//be capable of keeping track of more then 1 progress state at any given time
public class ProgressBarCrafting : NetworkBehaviour
{
	public Sprite[] progressSprites;

	private SpriteRenderer spriteRenderer;

	//only useful serverside:
	private List<PlayerProgressCrafting> playerProgress = new List<PlayerProgressCrafting>();

	void Start()
	{
		spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		spriteRenderer.gameObject.SetActive(false);
	}

	///<Summary>
	/// To use send the worldpos, time it should take to complete, the
	/// ServerSide method that should be called on completion and the
	/// Player the progress bar is for.
	///</Summary>

	[Server]
	public void StartProgress(Vector3 pos, float timeForCompletion,
		Action serverCompletedAction, GameObject _player)
	{
		var _playerSprites = _player.GetComponent<PlayerSprites>();
		playerProgress.Add(new PlayerProgressCrafting
		{
			player = _player,
				timeToFinish = timeForCompletion,
				completedAction = serverCompletedAction,
				position = pos,
				playerSprites = _playerSprites,
				playerPositionCache = _player.transform.position,
				facingDirectionCache = _playerSprites.currentDirection
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
			}

			//Cancel the progress bar if the player moves away or faces another direction:
			if (playerProgress[i].HasMovedAway())
			{
				CloseProgressBar(playerProgress[i]);
				continue;
			}

			//Finished! Invoke the action and close the progress bar for the player
			if (playerProgress[i].progress >= playerProgress[i].timeToFinish)
			{
				playerProgress[i].completedAction.Invoke();
				CloseProgressBar(playerProgress[i]);
			}
		}
	}

	private void CloseProgressBar(PlayerProgressCrafting playerProg)
	{
		//Notify player to turn off progress bar:
		ProgressBarMessage.Send(playerProg.player, -1, playerProg.position);
		//remove from the player progress list:
		playerProgress.Remove(playerProg);
	}
}

public class PlayerProgressCrafting
{
	public float progress = 0f;
	public float timeToFinish;
	public GameObject player;
	public PlayerSprites playerSprites;
	public Vector3 playerPositionCache;
	public Orientation facingDirectionCache;
	public Action completedAction;
	public Vector3 position;
	public float progUnit { get { return timeToFinish / 21f; } }
	public int spriteIndex { get { return Mathf.Clamp((int) (progress / progUnit), 0, 21); } }
	public int lastSpriteIndex = 0;
	public bool timeToNotifyPlayer { get { return lastSpriteIndex != spriteIndex; } }

	//has the player moved away while the progress bar is in progress?
	public bool HasMovedAway()
	{
		if (playerSprites.currentDirection != facingDirectionCache ||
			player.transform.position != playerPositionCache)
		{
			return true;
		}
		return false;
	}

}