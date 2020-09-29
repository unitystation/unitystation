using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// A generic drawer component designed for multi-tile drawer objects.
/// </summary>
[RequireComponent(typeof(ObjectBehaviour))] // For setting held items' containers to the drawer.
[ExecuteInEditMode]
public class Drawer : NetworkBehaviour, IServerDespawn, ICheckedInteractable<HandApply>
{
	protected enum DrawerState
	{
		Open = 0,
		Shut = 1
	}

	protected enum SpriteOrientation
	{
		South = 0,
		North = 1,
		East = 2,
		West = 3
	}

	protected RegisterObject registerObject;
	protected Directional directional;
	protected PushPull drawerPushPull;
	protected SpriteHandler drawerSpriteHandler;

	protected Matrix Matrix => registerObject.Matrix;
	protected Vector3Int DrawerWorldPosition => registerObject.WorldPosition;
	// This script uses Matrix.Get(), which requires a local position, but Spawn requires a world position.
	protected Vector3Int TrayWorldPosition => GetTrayPosition(DrawerWorldPosition); // Spawn requires world position
	protected Vector3Int TrayLocalPosition => ((Vector3)TrayWorldPosition).ToLocalInt(Matrix);

	protected GameObject tray;
	protected CustomNetTransform trayTransform;
	protected ObjectBehaviour trayBehaviour;
	protected SpriteHandler traySpriteHandler;

	[SerializeField]
	[Tooltip("The corresponding tray that the drawer will spawn.")]
	protected GameObject trayPrefab = default;
	[SerializeField]
	[Tooltip("Whether the drawer can store players.")]
	protected bool storePlayers = true;

	protected DrawerState drawerState = DrawerState.Shut;

	// Inventory
	// Using a dictionary for held items so we can have a messy drawer by keeping their original vectors.
	protected Dictionary<ObjectBehaviour, Vector3> serverHeldItems = new Dictionary<ObjectBehaviour, Vector3>();
	protected List<ObjectBehaviour> serverHeldPlayers = new List<ObjectBehaviour>();

	#region Init Methods

	protected virtual void Awake()
	{
		registerObject = GetComponent<RegisterObject>();
		directional = GetComponent<Directional>();
		drawerPushPull = GetComponent<PushPull>();
		drawerSpriteHandler = GetComponentInChildren<SpriteHandler>();
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		registerObject = GetComponent<RegisterObject>();
		registerObject.WaitForMatrixInit(ServerInit);
		directional.OnDirectionChange.AddListener(OnDirectionChanged);
	}

	void ServerInit(MatrixInfo matrixInfo)
	{
		SpawnResult traySpawn = Spawn.ServerPrefab(trayPrefab, DrawerWorldPosition);
		if (!traySpawn.Successful)
		{
			Logger.LogError($"Failed to spawn tray! Is {name} prefab missing reference to {nameof(traySpawn)} prefab?");
			return;
		}
		tray = traySpawn.GameObject;

		tray.GetComponent<InteractableDrawerTray>().parentDrawer = this;
		traySpriteHandler = tray.GetComponentInChildren<SpriteHandler>();
		trayTransform = tray.GetComponent<CustomNetTransform>();
		trayBehaviour = tray.GetComponent<ObjectBehaviour>();
		trayBehaviour.parentContainer = drawerPushPull;
		trayBehaviour.VisibleState = false;

		UpdateSpriteState();
		UpdateSpriteOrientation();
	}

	#endregion Init Methods

	/// <summary>
	/// If the object is about to despawn, eject its contents (unless already open)
	/// so they are not stranded at HiddenPos.
	/// </summary>
	/// <param name="despawnInfo"></param>
	public void OnDespawnServer(DespawnInfo despawnInfo)
	{
		if (drawerState == DrawerState.Open) return;

		EjectItems(true);
		EjectPlayers(true);
		Despawn.ServerSingle(tray);
	}

	#region Sprite

	private void OnDirectionChanged(Orientation newDirection)
	{
		UpdateSpriteOrientation();
	}

	public void OnEditorDirectionChange()
	{
		UpdateSpriteOrientation();
	}

	/// <summary>
	/// Updates the drawer to the given state and sets the sprites accordingly.
	/// </summary>
	/// <param name="newState">The new state the drawer should be set to</param>
	protected void SetDrawerState(DrawerState newState)
	{
		drawerState = newState;
		UpdateSpriteState();
	}

	private void UpdateSpriteState()
	{
		drawerSpriteHandler.ChangeSprite((int)drawerState);
	}

	private void UpdateSpriteOrientation()
	{
		int spriteVariant = (int)GetSpriteDirection();
		drawerSpriteHandler.ChangeSpriteVariant(spriteVariant);

		if (traySpriteHandler != null)
		{
			traySpriteHandler.ChangeSpriteVariant(spriteVariant);
		}
	}

	private SpriteOrientation GetSpriteDirection()
	{
		switch (directional.CurrentDirection.AsEnum())
		{
			case OrientationEnum.Up: return SpriteOrientation.North;
			case OrientationEnum.Down: return SpriteOrientation.South;
			case OrientationEnum.Left: return SpriteOrientation.West;
			case OrientationEnum.Right: return SpriteOrientation.East;
			default: return SpriteOrientation.South;
		}
	}

	#endregion Sprite

	#region Interactions

	public virtual bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.HandObject != null) return false;

		return true;
	}

	public virtual void ServerPerformInteraction(HandApply interaction)
	{
		if (drawerState == DrawerState.Open) CloseDrawer();
		else OpenDrawer();
	}

	#endregion Interactions

	/// <summary>
	/// Returns the tray position from the current orientation and the given drawer position.
	/// </summary>
	/// <param name="drawerPosition">The drawer position</param>
	/// <returns>The tray position</returns>
	protected Vector3Int GetTrayPosition(Vector3Int drawerPosition)
	{
		return (drawerPosition + directional.CurrentDirection.Vector).CutToInt();
	}

	#region Server Only

	public virtual void OpenDrawer()
	{
		trayBehaviour.parentContainer = null;
		trayTransform.SetPosition(TrayWorldPosition);

		EjectItems();
		EjectPlayers();

		SoundManager.PlayNetworkedAtPos("BinOpen", DrawerWorldPosition, Random.Range(0.8f, 1.2f), sourceObj: gameObject);
		SetDrawerState(DrawerState.Open);
	}

	public virtual void CloseDrawer()
	{
		trayBehaviour.parentContainer = drawerPushPull;
		trayBehaviour.VisibleState = false;

		GatherItems();
		if (storePlayers) GatherPlayers();

		SoundManager.PlayNetworkedAtPos("BinClose", DrawerWorldPosition, Random.Range(0.8f, 1.2f), sourceObj: gameObject);
		SetDrawerState(DrawerState.Shut);
	}

	/// <summary>
	/// Ejects items when drawer is opened or despawned. If action is despawning, set drawerDespawning true.
	/// </summary>
	/// <param name="drawerDespawning"></param>
	protected virtual void EjectItems(bool drawerDespawning = false)
	{
		Vector3 position = TrayWorldPosition;
		if (drawerDespawning) position = DrawerWorldPosition;

		foreach (KeyValuePair<ObjectBehaviour, Vector3> item in serverHeldItems)
		{
			item.Key.parentContainer = null;
			item.Key.GetComponent<CustomNetTransform>().SetPosition(position - item.Value);
		}

		serverHeldItems = new Dictionary<ObjectBehaviour, Vector3>();
	}

	protected virtual void GatherItems()
	{
		var items = Matrix.Get<ObjectBehaviour>(TrayLocalPosition, ObjectType.Item, true);
		foreach (ObjectBehaviour item in items)
		{
			// Other position fields such as registerObject.WorldPosition seem to give tile integers.
			var tileOffsetPosition = TrayWorldPosition - item.transform.position;
			serverHeldItems.Add(item, tileOffsetPosition);
			item.parentContainer = drawerPushPull;
			item.VisibleState = false;
		}
	}

	/// <summary>
	/// Ejects players when drawer is opened or despawned. If action is despawning, set drawerDespawning true.
	/// </summary>
	/// <param name="drawerDespawning"></param>
	protected virtual void EjectPlayers(bool drawerDespawning = false)
	{
		Vector3 position = TrayWorldPosition;
		if (drawerDespawning) position = DrawerWorldPosition;

		foreach (ObjectBehaviour player in serverHeldPlayers)
		{
			player.parentContainer = null;
			player.GetComponent<PlayerSync>().SetPosition(position);

			//Stop tracking the drawer
			FollowCameraMessage.Send(player.gameObject, player.gameObject);
		}

		serverHeldPlayers = new List<ObjectBehaviour>();
	}

	protected virtual void GatherPlayers()
	{
		var players = Matrix.Get<ObjectBehaviour>(TrayLocalPosition, ObjectType.Player, true);
		foreach (ObjectBehaviour player in players)
		{
			serverHeldPlayers.Add(player);
			player.parentContainer = drawerPushPull;
			player.VisibleState = false;

			// Start tracking the drawer
			var playerScript = player.GetComponent<PlayerScript>();
			if (!playerScript.IsGhost) FollowCameraMessage.Send(player.gameObject, gameObject);
		}
	}

	#endregion Server Only
}
