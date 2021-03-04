using UnityEngine;
using Mirror;
using Systems.GhostRoles;
using System;
using ScriptableObjects;

public class ReinforcementTeleporter : MonoBehaviour, ICheckedInteractable<HandActivate>
{

	bool WasUsed = false;

	[SerializeField] private GhostRoleData ghostRole = default;

	private uint createdRoleKey;

	public event Action OnGhostRoleTimeout;


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
		role.OnTimerExpired += OnGhostRoleTimeout;
	}

	private void SpawnReinforcement(ConnectedPlayer player)
	{
		player.Script.playerNetworkActions.ServerRespawnPlayerAntag(player, "Nuclear Operative");
		WasUsed = true;
		player.Script.PlayerSync.SetPosition(this.gameObject.AssumedWorldPosServer(), true);
	}

	public void ClearGhostRole()
	{
		GhostRoleManager.Instance.ServerRemoveRole(createdRoleKey);
	}


}