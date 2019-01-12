using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Provides central access to the Players Health
/// </summary>
public class PlayerHealth : LivingHealthBehaviour
{
	private PlayerMove playerMove;

	private PlayerNetworkActions playerNetworkActions;

	public bool serverPlayerConscious { get; set; } = true; //Only used on the server

	public override void OnStartClient()
	{
		playerNetworkActions = GetComponent<PlayerNetworkActions>();
		playerMove = GetComponent<PlayerMove>();

		PlayerScript playerScript = GetComponent<PlayerScript>();

		if (playerScript.JobType == JobType.NULL)
		{
			foreach (Transform t in transform)
			{
				t.gameObject.SetActive(false);
			}
			ConsciousState = ConsciousState.DEAD;

			// Fixme: No more setting allowInputs on client:
			// When job selection screen is removed from round start 
			// (and moved to preference system in lobby) then we can remove this
			playerMove.allowInput = false;
		}

		base.OnStartClient();
	}

	protected override void OnDeathActions()
	{
		if (CustomNetworkManager.Instance._isServer)
		{
			PlayerNetworkActions pna = gameObject.GetComponent<PlayerNetworkActions>();
			PlayerMove pm = gameObject.GetComponent<PlayerMove>();

			ConnectedPlayer player = PlayerList.Instance.Get(gameObject);

			string killerName = "Stressful work";
			if (LastDamagedBy != null)
			{
				killerName = PlayerList.Instance.Get(LastDamagedBy).Name;
			}

			string playerName = player.Name ?? "dummy";
			if (killerName == playerName)
			{
				PostToChatMessage.Send(playerName + " commited suicide", ChatChannel.System); //Killfeed
			}
			else if (killerName.EndsWith(playerName))
			{
				// chain reactions
				PostToChatMessage.Send(
					playerName + " screwed himself up with some help (" + killerName + ")",
					ChatChannel.System); //Killfeed
			}
			else
			{
				PlayerList.Instance.UpdateKillScore(LastDamagedBy, gameObject);
			}
			pna.DropItem("rightHand");
			pna.DropItem("leftHand");

			if (isServer)
			{
				EffectsFactory.Instance.BloodSplat(transform.position, BloodSplatSize.large);
			}

			PlayerDeathMessage.Send(gameObject);
			//syncvars for everyone
			pm.isGhost = true;
			pm.allowInput = true;
			//consider moving into PlayerDeathMessage.Process()
			pna.RpcSpawnGhost();
			RpcPassBullets(gameObject);

			//FIXME Remove for next demo
			pna.RespawnPlayer(10);
		}
	}

	[ClientRpc]
	private void RpcPassBullets(GameObject target)
	{
		foreach (BoxCollider2D comp in target.GetComponents<BoxCollider2D>())
		{
			if (!comp.isTrigger)
			{
				comp.enabled = false;
			}
		}
	}

	///     make player unconscious upon crit
	protected override void OnCritActions()
	{
		playerNetworkActions.SetConsciousState(false);
	}
}