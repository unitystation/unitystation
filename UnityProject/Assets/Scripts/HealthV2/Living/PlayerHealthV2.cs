using Mirror;

public class PlayerHealthV2 : LivingHealthMasterBase
{
	public PlayerMove PlayerMove;
	private PlayerSprites playerSprites;

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
}
