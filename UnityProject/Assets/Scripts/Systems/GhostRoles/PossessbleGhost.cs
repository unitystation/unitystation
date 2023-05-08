using System;
using System.Collections;
using System.Collections.Generic;
using ScriptableObjects;
using Systems.GhostRoles;
using UnityEngine;

public class PossessbleGhost : MonoBehaviour
{

	//0 if theres a player in body
	private uint createdRoleKey;

	private PlayerInfo playerTookOver;

	public GhostRoleData roleData;

	public SpriteHandler SpriteHandler;

	public SpriteDataSO SpriteOccupied;

	public SpriteDataSO SpriteUnclaimed;

	public void Start()
	{
		var Possessedble = this.GetComponent<IPlayerPossessable>();
		if (Possessedble.PossessingMind == null)
		{
			if (SpriteHandler != null)
			{
				SpriteHandler.SetSpriteSO(SpriteUnclaimed);
			}
			SetUpGhostRole();
		}
		else
		{
			if (SpriteHandler != null)
			{
				SpriteHandler.SetSpriteSO(SpriteOccupied);
			}
		}

		Possessedble.OnActionPossess += OnPlayerPossessing;


	}

	public void OnDestroy()
	{
		if (createdRoleKey != 0)
		{
			GhostRoleManager.Instance.ServerRemoveRole(createdRoleKey);
		}
	}


	public void OnPlayerPossessing()
	{
		if (createdRoleKey != 0)
		{
			GhostRoleManager.Instance.ServerRemoveRole(createdRoleKey);
			if (SpriteHandler != null)
			{
				SpriteHandler.SetSpriteSO(SpriteOccupied);
			}
		}
	}


	private void SetUpGhostRole()
	{
		if (createdRoleKey != 0)
		{
			GhostRoleManager.Instance.ServerRemoveRole(createdRoleKey);
		}

		var Possessedble = this.GetComponent<IPlayerPossessable>();

		//Remove current player
		if (Possessedble.PossessingMind != null)
		{
			if (Possessedble.PossessingMind.GetCurrentMob().OrNull()?.GetComponent<PlayerScript>().IsGhost == false)
			{
				//Force player current into ghost
				Possessedble.PossessingMind.Ghost();
			}
		}

		createdRoleKey = GhostRoleManager.Instance.ServerCreateRole(roleData);
		var role = GhostRoleManager.Instance.serverAvailableRoles[createdRoleKey];
		role.OnPlayerAdded += OnSpawnFromGhostRole;
	}


	private void OnSpawnFromGhostRole(PlayerInfo player)
	{
		//Sanity check
		if(createdRoleKey == 0) return;

		playerTookOver = player;

		//Transfer player chosen into body
		player.Mind.SetPossessingObject(gameObject);

		//Remove the player so they can join again once they die
		GhostRoleManager.Instance.ServerRemoveWaitingPlayer(createdRoleKey, player);

		//GhostRoleManager will remove role don't need to call RemoveGhostRole
		createdRoleKey = 0;

		//PlayerTookOver only needs to be set for ServerTransferPlayerToNewBody as OnPlayerTransfer is triggered
		//During it
		playerTookOver = null;

		player.Mind.StopGhosting();
		if (SpriteHandler != null)
		{
			SpriteHandler.SetSpriteSO(SpriteOccupied);
		}
	}


}
