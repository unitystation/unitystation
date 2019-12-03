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
	private PlayerMove playerMove;

	private PlayerNetworkActions playerNetworkActions;
	/// <summary>
	/// Cached register player
	/// </summary>
	private RegisterPlayer registerPlayer;

	private ItemStorage itemStorage;

	//fixme: not actually set or modified. keep an eye on this!
	public bool serverPlayerConscious { get; set; } = true; //Only used on the server

	private void Awake()
	{
		base.Awake();

		OnConsciousStateChangeServer.AddListener(OnPlayerConsciousStateChangeServer);
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
				Chat.AddActionMsgToChat(gameObject, $" You screwed yourself up with some help from {killerName}",
					$"{playerName} screwed himself up with some help from {killerName}");
			}
			else
			{
				PlayerList.Instance.TrackKill(LastDamagedBy, gameObject);
			}

			//drop items in hand
			Inventory.ServerDrop(itemStorage.GetNamedItemSlot(NamedSlot.leftHand));
			Inventory.ServerDrop(itemStorage.GetNamedItemSlot(NamedSlot.rightHand));

			if (isServer)
			{
				EffectsFactory.BloodSplat(transform.position, BloodSplatSize.large, bloodColor);
				string descriptor = player.Script.characterSettings.PossessivePronoun();
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

	protected override void Gib()
	{
		EffectsFactory.BloodSplat( transform.position, BloodSplatSize.large, bloodColor );
		//drop clothes, gib... but don't destroy actual player, a piece should remain

		//drop everything
		foreach (var slot in itemStorage.GetItemSlots())
		{
			Inventory.ServerDrop(slot);
		}

		if (!playerMove.PlayerScript.IsGhost)
		{ //dirty way to follow gibs. change this when implementing proper gibbing, perhaps make it follow brain
			var gibsToFollow = MatrixManager.GetAt<RawMeat>( transform.position.CutToInt(), true );
			if ( gibsToFollow.Count > 0 )
			{
				var gibs = gibsToFollow[0];
				FollowCameraMessage.Send(gameObject, gibs.gameObject);
				var gibsIntegrity = gibs.GetComponent<Integrity>();
				if ( gibsIntegrity != null )
				{	//Stop cam following gibs if they are destroyed
					gibsIntegrity.OnWillDestroyServer.AddListener( x => FollowCameraMessage.Send( gameObject, null ) );
				}
			}
		}
		playerMove.PlayerScript.pushPull.VisibleState = false;
	}

	///     make player unconscious upon crit
	private void OnPlayerConsciousStateChangeServer( ConsciousState oldState, ConsciousState newState )
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