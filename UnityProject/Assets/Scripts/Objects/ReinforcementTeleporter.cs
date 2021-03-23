using UnityEngine;
using Mirror;
using Systems.GhostRoles;
using System;
using System.Collections;
using ScriptableObjects;

public class ReinforcementTeleporter : MonoBehaviour, ICheckedInteractable<HandActivate>
{

	bool WasUsed = false;

	[SerializeField] private GhostRoleData ghostRole = default;

	private uint createdRoleKey;

	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		if (WasUsed) return false;
		return true;
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		CreateGhostRole();
	}

	public void CreateGhostRole()
	{
		if (GhostRoleManager.Instance.serverAvailableRoles.ContainsKey(createdRoleKey))
		{
				return;
		}
		else if (WasUsed)
		{
			return;
		}
		createdRoleKey = GhostRoleManager.Instance.ServerCreateRole(ghostRole);
		GhostRoleServer role = GhostRoleManager.Instance.serverAvailableRoles[createdRoleKey];

		role.OnPlayerAdded += SpawnReinforcement;
		role.OnTimerExpired += ClearGhostRole;
	}

	private void SpawnReinforcement(ConnectedPlayer player)
	{
		player.Script.playerNetworkActions.ServerRespawnPlayerAntag(player, "Nuclear Operative");
		WasUsed = true;
		StartCoroutine(TeleportOnSpawn(player));
	}

	IEnumerator TeleportOnSpawn(ConnectedPlayer player)
	{
		//Waits until the player is no longer a ghost...
		while (player.Script.IsGhost)
		{
			yield return WaitFor.EndOfFrame;
		}

		player.Script.PlayerSync.SetPosition(this.gameObject.AssumedWorldPosServer(), true);

	}

	public void ClearGhostRole()
	{
		GhostRoleManager.Instance.ServerRemoveRole(createdRoleKey);
	}


}