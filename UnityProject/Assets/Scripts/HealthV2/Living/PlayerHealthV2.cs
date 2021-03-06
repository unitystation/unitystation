using Mirror;

public class PlayerHealthV2 : LivingHealthMasterBase
{
	public PlayerMove PlayerMove;
	private PlayerSprites playerSprites;

	public PlayerScript PlayerScript;

	private PlayerNetworkActions playerNetworkActions;
	/// <summary>
	/// Cached register player
	/// </summary>
	private RegisterPlayer registerPlayer;
	public RegisterPlayer RegPlayer => registerPlayer;

	private Equipment equipment;
	public Equipment Equip => equipment;

	private ItemStorage itemStorage;

	private bool init = false;

	//fixme: not actually set or modified. keep an eye on this!
	public bool serverPlayerConscious { get; set; } = true; //Only used on the server

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
		equipment = GetComponent<Equipment>();
		PlayerScript = GetComponent<PlayerScript>();
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

	[Server]
	public void ServerGibPlayer()
	{
		Gib();
	}

	protected override void Gib()
	{
		Death();
		//EffectsFactory.BloodSplat(transform.position, BloodSplatSize.large, bloodColor);
		//drop clothes, gib... but don't destroy actual player, a piece should remain

		//drop everything
		foreach (var slot in itemStorage.GetItemSlots())
		{
			Inventory.ServerDrop(slot);
		}

		PlayerMove.PlayerScript.pushPull.VisibleState = false;
		playerNetworkActions.ServerSpawnPlayerGhost();
	}

	protected override void OnDeathActions()
	{
		if (CustomNetworkManager.Instance._isServer)
		{
			ConnectedPlayer player = PlayerList.Instance.Get(gameObject);

			// string killerName = null;
			// if (LastDamagedBy != null)
			// {
				// var lastDamager = PlayerList.Instance.Get(LastDamagedBy);
				// if (lastDamager != null)
				// {
					// killerName = lastDamager.Name;
					// AutoMod.ProcessPlayerKill(lastDamager, player);
				// }
			// }

			// if (killerName == null)
			// {
				// killerName = "Stressful work";
			// }

			// string playerName = player?.Name ?? "dummy";
			// if (killerName == playerName)
			// {
				// Chat.AddActionMsgToChat(gameObject, "You committed suicide, what a waste.", $"{playerName} committed suicide.");
			// }
			// else if (killerName.EndsWith(playerName))
			// {
				// chain reactions
				// Chat.AddActionMsgToChat(gameObject, $" You screwed yourself up with some help from {killerName}",
					// $"{playerName} screwed himself up with some help from {killerName}");
			// }
			// else
			// {
				// PlayerList.Instance.TrackKill(LastDamagedBy, gameObject);
			// }

			//drop items in hand
			if (itemStorage != null)
			{
				Inventory.ServerDrop(itemStorage.GetNamedItemSlot(NamedSlot.leftHand));
				Inventory.ServerDrop(itemStorage.GetNamedItemSlot(NamedSlot.rightHand));
			}

			if (isServer)
			{
				//EffectsFactory.BloodSplat(transform.position, BloodSplatSize.large, bloodColor);
				string descriptor = null;
				if (player != null)
				{
					descriptor = player.CharacterSettings?.TheirPronoun();
				}

				if (descriptor == null)
				{
					descriptor = "their";
				}

				Chat.AddLocalMsgToChat($"<b>{player.Name}</b> seizes up and falls limp, {descriptor} eyes dead and lifeless...", gameObject);
			}

			//PlayerDeathMessage.Send(gameObject);
		}
	}

}
