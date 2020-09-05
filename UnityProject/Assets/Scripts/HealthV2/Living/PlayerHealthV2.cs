using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealthV2 : LivingHealthMasterBase
{
	public PlayerMove PlayerMove;
	private PlayerSprites playerSprites;

	private PlayerNetworkActions playerNetworkActions;
	/// <summary>
	/// Cached register player
	/// </summary>
	private RegisterPlayer registerPlayer;

	private ItemStorage itemStorage;

	private bool init = false;

	public override void Awake()
	{
		base.Awake();
		EnsureInit();
	}


	void EnsureInit()
	{
		if (init) return;

		init = true;
		playerNetworkActions = GetComponent<PlayerNetworkActions>();
		PlayerMove = GetComponent<PlayerMove>();
		playerSprites = GetComponent<PlayerSprites>();
		registerPlayer = GetComponent<RegisterPlayer>();
		itemStorage = GetComponent<ItemStorage>();

		OnConsciousStateChangeServer.AddListener(OnPlayerConsciousStateChangeServer);
	}

	public override void OnStartClient()
	{
		EnsureInit();
		base.OnStartClient();
	}

	public override void OnStartServer()
	{
		EnsureInit();
		base.OnStartServer();
	}

	private void OnPlayerConsciousStateChangeServer(ConsciousState oldState, ConsciousState newState)
	{
		if (playerNetworkActions == null || registerPlayer == null) EnsureInit();

		if (isServer)
		{
			playerNetworkActions.OnConsciousStateChanged(oldState, newState);
		}

		//we stay upright if buckled or conscious
		registerPlayer.ServerSetIsStanding(newState == ConsciousState.CONSCIOUS || PlayerMove.IsBuckled);
	}

}
