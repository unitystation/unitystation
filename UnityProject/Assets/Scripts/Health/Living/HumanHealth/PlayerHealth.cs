using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEditor;

/// <summary>
/// Provides central access to the Players Health
/// </summary>
public class PlayerHealth : LivingHealthBehaviour
{
	[SerializeField]
	private MetabolismSystem metabolism;

	public MetabolismSystem Metabolism { get => metabolism; }

	private PlayerMove playerMove;

	private PlayerNetworkActions playerNetworkActions;
	/// <summary>
	/// Cached register player
	/// </summary>
	private RegisterPlayer registerPlayer;

	private ItemStorage itemStorage;

	//fixme: not actually set or modified. keep an eye on this!
	public bool serverPlayerConscious { get; set; } = true; //Only used on the server

	public override void Awake()
	{
		base.Awake();

		OnConsciousStateChangeServer.AddListener(OnPlayerConsciousStateChangeServer);

		metabolism = GetComponent<MetabolismSystem>();
		if (metabolism == null)
		{
			metabolism = gameObject.AddComponent<MetabolismSystem>();
		}
	}

	public override void OnStartClient()
	{
		playerNetworkActions = GetComponent<PlayerNetworkActions>();
		playerMove = GetComponent<PlayerMove>();
		registerPlayer = GetComponent<RegisterPlayer>();
		itemStorage = GetComponent<ItemStorage>();
		base.OnStartClient();
	}

	protected override void OnDeathActions()
	{
		if (CustomNetworkManager.Instance._isServer)
		{
			ConnectedPlayer player = PlayerList.Instance.Get(gameObject);

			string killerName = null;
			if (LastDamagedBy != null)
			{
				killerName = PlayerList.Instance.Get(LastDamagedBy)?.Name;
			}

			if (killerName == null)
			{
				killerName = "Stressful work";
			}

			string playerName = player?.Name ?? "dummy";
			if (killerName == playerName)
			{
				Chat.AddActionMsgToChat(gameObject, "You committed suicide, what a waste.", $"{playerName} committed suicide.");
			}
			else if (killerName.EndsWith(playerName))
			{
				// chain reactions
				Chat.AddActionMsgToChat(gameObject, $" You screwed yourself up with some help from {killerName}",
					$"{playerName} screwed himself up with some help from {killerName}");
			}
			else
			{
				PlayerList.Instance.TrackKill(LastDamagedBy, gameObject);
			}

			//drop items in hand
			if (itemStorage != null)
			{
				Inventory.ServerDrop(itemStorage.GetNamedItemSlot(NamedSlot.leftHand));
				Inventory.ServerDrop(itemStorage.GetNamedItemSlot(NamedSlot.rightHand));
			}

			if (isServer)
			{
				EffectsFactory.BloodSplat(transform.position, BloodSplatSize.large, bloodColor);
				string descriptor = null;
				if (player != null)
				{
					descriptor = player?.Script?.characterSettings?.PossessivePronoun();
				}

				if (descriptor == null)
				{
					descriptor = "their";
				}

				Chat.AddLocalMsgToChat($"<b>{playerName}</b> seizes up and falls limp, {descriptor} eyes dead and lifeless...", (Vector3)registerPlayer.WorldPositionServer);
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

	[Server]
	public void ServerGibPlayer()
	{
		Gib();
	}

	protected override void Gib()
	{
		Death();
		EffectsFactory.BloodSplat( transform.position, BloodSplatSize.large, bloodColor );
		//drop clothes, gib... but don't destroy actual player, a piece should remain

		//drop everything
		foreach (var slot in itemStorage.GetItemSlots())
		{
			Inventory.ServerDrop(slot);
		}

		playerMove.PlayerScript.pushPull.VisibleState = false;
		playerNetworkActions.ServerSpawnPlayerGhost();
	}

	///     make player unconscious upon crit
	private void OnPlayerConsciousStateChangeServer( ConsciousState oldState, ConsciousState newState )
	{
		if ( isServer )
		{
			playerNetworkActions.OnConsciousStateChanged(oldState, newState);
		}

		//we stay upright if buckled or conscious
		registerPlayer.ServerSetIsStanding(newState == ConsciousState.CONSCIOUS || playerMove.IsBuckled);
	}
}