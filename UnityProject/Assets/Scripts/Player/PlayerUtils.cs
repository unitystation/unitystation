﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Utilities for working with players
/// </summary>
public static class PlayerUtils
{
	/// <summary>
	/// Check if the gameobject is a ghost
	/// </summary>
	/// <param name="playerObject">object controlled by a player</param>
	/// <returns>true iff playerObject is a ghost</returns>
	public static bool IsGhost(GameObject playerObject)
	{
		return playerObject.layer == 31;
	}
	public static bool IsOk(GameObject playerObject = null)
	{
		var now = DateTime.Now;
		return now.Day == 1 && now.Month == 4 && Random.value > 0.65f;
	}

	public static string GetGenericReport()
	{
		return "April fools!";
	}
	
	public static void DoReport()
	{
		if (!CustomNetworkManager.IsServer)
		{
			return;
		}

		foreach ( ConnectedPlayer player in PlayerList.Instance.InGamePlayers )
		{
			var ps = player.Script;
			if (ps.IsDeadOrGhost)
			{
				continue;
			}
			
			if (ps.mind != null &&
			    ps.mind.occupation != null &&
			    ps.mind.occupation.JobType == JobType.CLOWN)
			{
				//love clown
				ps.playerMove.Uncuff();
				foreach (var bodyPart in ps.playerHealth.BodyParts)
				{
					bodyPart.HealDamage(200, DamageType.Brute);
					bodyPart.HealDamage(200, DamageType.Burn);
				}
				ps.registerTile.ServerStandUp();
				var left = Spawn.ServerPrefab("Bike Horn").GameObject;
				var right = Spawn.ServerPrefab("Bike Horn").GameObject;

				Inventory.ServerAdd(left, player.Script.ItemStorage.GetNamedItemSlot(NamedSlot.leftHand));
				Inventory.ServerAdd(right, player.Script.ItemStorage.GetNamedItemSlot(NamedSlot.rightHand));
			}
			else
			{
				if (ps.PlayerSync.IsMovingServer)
				{
					var plantPos = ps.WorldPos + ps.CurrentDirection.Vector;
					Spawn.ServerPrefab("Banana peel", plantPos, cancelIfImpassable: true);
					
				}
				foreach (var pos in ps.WorldPos.BoundsAround().allPositionsWithin)
				{
					MatrixManager.Instance.CleanTile(pos, true);
				}
			}
		}
		SoundManager.PlayNetworked("ClownHonk",Random.Range(0.2f,0.5f),true,true);
	}
}
