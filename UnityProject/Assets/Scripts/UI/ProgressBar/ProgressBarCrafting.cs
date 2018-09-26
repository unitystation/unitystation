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
	public Sprite[] progressSprites; //21 sprites = 4.77 per unit out of 100;

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
		playerProgress.Add(new PlayerProgressCrafting{
			player = _player,
			completedAction = serverCompletedAction
		});

		//Start the progress for the player:
		ProgressBarMessage.Send(_player, 0, pos);
	}

	public void ClientUpdateProgress(Vector3 pos, int spriteIndex)
	{
		if(!spriteRenderer.gameObject.activeInHierarchy){
			spriteRenderer.gameObject.SetActive(true);
		}

		// -1 sent from server means the crafting is complete. dismiss the progress bar:
		if(spriteIndex == -1){
			spriteRenderer.gameObject.SetActive(false);
			return;
		}

		transform.position = pos;
		spriteRenderer.sprite = progressSprites[spriteIndex];
	}
}

public class PlayerProgressCrafting
{
	public float progress = 0f;
	public GameObject player;
	public Action completedAction;

}