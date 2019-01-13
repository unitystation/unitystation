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
		string _additionalSfx = "", float _additionalSfxPitch = 1f)
	{
		var _playerSprites = _player.GetComponent<PlayerSprites>();
		playerProgress.Add(new PlayerProgressEntry
		{
			player = _player,
				timeToFinish = timeForCompletion,
				completedAction = finishProgressAction,
				position = pos,
				playerSprites = _playerSprites,
				playerPositionCache = _player.transform.position,
				facingDirectionCache = _playerSprites.currentDirection,
				additionalSfx = _additionalSfx,
				additionalSfxPitch = _additionalSfxPitch
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
				CloseProgressBar(playerProgress[i]);
				continue;
			}

			//Finished! Invoke the action and close the progress bar for the player
			if (playerProgress[i].progress >= playerProgress[i].timeToFinish)
			{
				playerProgress[i].completedAction.DoAction();
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
	public PlayerSprites playerSprites;
	public Vector3 playerPositionCache;
	public Orientation facingDirectionCache;
	public FinishProgressAction completedAction;
	public Vector3 position;
	public float progUnit { get { return timeToFinish / 21f; } }
	public int spriteIndex { get { return Mathf.Clamp((int) (progress / progUnit), 0, 20); } }
	public int lastSpriteIndex = 0;
	public bool timeToNotifyPlayer { get { return lastSpriteIndex != spriteIndex; } }
	public string additionalSfx = "";  // leave empty if you don't want one to play (plays at sprite index 12)
	public float additionalSfxPitch = 1f;

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

	public void PlayAdditionalSound()
	{
		if (!string.IsNullOrEmpty(additionalSfx))
		{
			PlaySoundMessage.SendToAll(additionalSfx, position, additionalSfxPitch);
		}
	}
}

public class FinishProgressAction
{
	public enum Action
	{
		TileConstruction,
		TileDeconstruction,
		CleanTile,
		//Add whatever else you need here
	}

	private Action actionType;

	//Tile change stuff:
	private TileChangeManager tileChangeManager;
	private TileType tileType;
	private Vector3 cellPos;
	private Vector3 worldPos; //worldPos of the action or tile
	private MopTrigger theMop;

	private GameObject originator;

	//Create a constructor for each new use type of FinishProgressAction (i.e you might add an Action type called HandCuff)
	public FinishProgressAction(Action action, TileChangeManager _tileChangeManager,
		TileType _tileType, Vector3 _cellPos, Vector3 _worldPos, GameObject _originator)
	{
		actionType = action;
		tileChangeManager = _tileChangeManager;
		tileType = _tileType;
		cellPos = _cellPos;
		worldPos = _worldPos;
		originator = _originator;
	}
	public FinishProgressAction(FinishProgressAction.Action cleanTile, Vector3 splatsPos, MopTrigger mop)
	{
		actionType = cleanTile;
		worldPos = splatsPos;
		theMop = mop;
	}
	public void DoAction()
	{
		switch (actionType)
		{
			case Action.TileConstruction:
				DoTileConstruction();
				break;
			case Action.TileDeconstruction:
				DoTileDeconstruction();
				break;
			case Action.CleanTile:
				DoCleanTile();
				break;
		}
	}

	private void DoTileConstruction()
	{
		//TODO
	}

	private void DoTileDeconstruction()
	{
		CraftingManager.Deconstruction.TryTileDeconstruct(
			tileChangeManager, tileType, cellPos, worldPos);
	}

	private void DoCleanTile()
	{
		theMop.CleanTile(worldPos);
	}
}