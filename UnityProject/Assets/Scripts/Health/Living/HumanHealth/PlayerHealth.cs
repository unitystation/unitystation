using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Provides central access to the Players Health
/// </summary>
public class PlayerHealth : LivingHealthBehaviour
{
	private PlayerMove playerMove;

	private PlayerNetworkActions playerNetworkActions;
	/// <summary>
	/// Cached register player
	/// </summary>
	private RegisterPlayer registerPlayer;

	//fixme: not actually set or modified. keep an eye on this!
	public bool serverPlayerConscious { get; set; } = true; //Only used on the server

	public override void OnStartClient()
	{
		playerNetworkActions = GetComponent<PlayerNetworkActions>();
		playerMove = GetComponent<PlayerMove>();
		registerPlayer = GetComponent<RegisterPlayer>();
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
				Chat.AddActionMsgToChat(gameObject, "You committed suicide, what a waste.", $"{playerName} committed suicide.");
			}
			else if (killerName.EndsWith(playerName))
			{
				// chain reactions
				Chat.AddActionMsgToChat(gameObject, $" You screwed yourself up with some help (" + killerName + ")",
					"{playerName} screwed himself up with some help (" + killerName + ")");
			}
			else
			{
				PlayerList.Instance.UpdateKillScore(LastDamagedBy, gameObject);
			}
			pna.DropItem(EquipSlot.rightHand);
			pna.DropItem(EquipSlot.leftHand);

			if (isServer)
			{
				EffectsFactory.Instance.BloodSplat(transform.position, BloodSplatSize.large);
			}

			PlayerDeathMessage.Send(gameObject);
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
	protected override void OnConsciousStateChange( ConsciousState oldState, ConsciousState newState )
	{
		if ( isServer )
		{
			playerNetworkActions.OnConsciousStateChanged(oldState, newState);
		}

		if (newState != ConsciousState.CONSCIOUS)
		{
			registerPlayer.LayDown();
		}
		else
		{
			registerPlayer.GetUp();
		}

	}
}