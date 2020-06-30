using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// A generic drawer component designed for multi-tile drawer objects.
/// </summary>
[RequireComponent(typeof(PushPull))] // For setting held items' containers to the drawer.
[ExecuteInEditMode]
public class Drawer : NetworkBehaviour, IMatrixRotation, ICheckedInteractable<HandApply>, IServerDespawn//, System.IEquatable<>
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

	[SyncVar]
	protected GameObject tray;
	protected CustomNetTransform trayTransform;
	protected ObjectBehaviour trayBehaviour;
	protected SpriteHandler traySpriteHandler;

	[SerializeField]
	[Tooltip("The corresponding tray that the drawer will spawn.")]
	protected GameObject trayPrefab;
	[SerializeField]
	[Tooltip("Whether the drawer can store players.")]
	protected bool storePlayers = true;

	[SyncVar(hook = nameof(OnSyncDrawerState))]
	protected DrawerState drawerState;
	[SyncVar(hook = nameof(OnSyncOrientation))]
	protected Orientation drawerOrientation;

	// Inventory
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
	}

	void ServerInit(MatrixInfo matrixInfo)
	{
		SpawnResult traySpawn = Spawn.ServerPrefab(trayPrefab, DrawerWorldPosition);
		if (!traySpawn.Successful)
		{
			throw new MissingReferenceException($"Failed to spawn tray! Is {name} prefab missing reference to tray prefab?");
		}
		tray = traySpawn.GameObject;

		traySpriteHandler = tray.GetComponentInChildren<SpriteHandler>();
		trayTransform = tray.GetComponent<CustomNetTransform>();
		trayBehaviour = tray.GetComponent<ObjectBehaviour>();
		trayBehaviour.parentContainer = drawerPushPull;
		trayBehaviour.VisibleState = false;

		// These two will sync drawer state/orientation and render appropriate sprite
		drawerState = DrawerState.Shut;
		drawerOrientation = directional.CurrentDirection;
	}

	public override void OnStartClient()
	{
		StartCoroutine(WaitForTray());
		base.OnStartClient();
	}

	IEnumerator WaitForTray()
	{
		while (tray == null)
		{
			yield return WaitFor.EndOfFrame;
		}

		traySpriteHandler = tray.GetComponentInChildren<SpriteHandler>();
		UpdateSpriteDirection();
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

	/// <summary>
	/// As per IMatrixRotate interface - called when matrix is rotated, updates drawer orientation.
	/// </summary>
	public void OnMatrixRotate(MatrixRotationInfo rotationInfo)
	{
		if (rotationInfo.IsClientside) return;
		if (!rotationInfo.IsEnding) return;

		drawerOrientation = directional.CurrentDirection;
	}

	#region Sprite

	/// <summary>
	/// Called when drawerState [SyncVar] variable is altered or the client has just joined.
	/// </summary>
	/// <param name="oldState"></param>
	/// <param name="newState"></param>
	protected virtual void OnSyncDrawerState(DrawerState oldState, DrawerState newState)
	{
		drawerState = newState;
		drawerSpriteHandler.ChangeSprite((int)drawerState);
	}

	/// <summary>
	/// Called when drawerOrientation [SyncVar] variable is altered or the client has just joined.
	/// </summary>
	/// <param name="oldState"></param>
	/// <param name="newState"></param>
	protected void OnSyncOrientation(Orientation oldState, Orientation newState)
	{
		// True when late client joins - [SyncVar] occured, but only
		// usable after OnStartClient(). Hence manual call in OnStartClient().
		if (traySpriteHandler == null) return;

		drawerOrientation = newState;
		UpdateSpriteDirection();
	}

	public void OnEditorDirectionChange()
	{
		drawerOrientation = directional.InitialOrientation;
		int spriteVariant = GetSpriteDirectionVariant();
		drawerSpriteHandler.ChangeSprite((int)DrawerState.Shut);
		drawerSpriteHandler.ChangeSpriteVariant(spriteVariant);
	}

	private int GetSpriteDirectionVariant()
	{
		if (drawerOrientation == Orientation.Up) return (int)SpriteOrientation.North;
		else if (drawerOrientation == Orientation.Down) return (int)SpriteOrientation.South;
		else if (drawerOrientation == Orientation.Left) return (int)SpriteOrientation.West;
		else return (int)SpriteOrientation.East;
	}

	private void UpdateSpriteDirection()
	{
		int spriteVariant = GetSpriteDirectionVariant();
		drawerSpriteHandler.ChangeSpriteVariant(spriteVariant);
		traySpriteHandler.ChangeSpriteVariant(spriteVariant);
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
	/// Returns the tray position from the given drawer position.
	/// </summary>
	/// <param name="vector">The drawer position</param>
	/// <returns>The tray position</returns>
	protected Vector3Int GetTrayPosition(Vector3Int vector)
	{
		if (drawerOrientation == Orientation.Up) vector += Vector3Int.up;
		else if (drawerOrientation == Orientation.Down) vector += Vector3Int.down;
		else if (drawerOrientation == Orientation.Left) vector += Vector3Int.left;
		else if (drawerOrientation == Orientation.Right) vector += Vector3Int.right;

		return vector;
	}

	#region Server Only

	protected virtual void OpenDrawer()
	{
		trayBehaviour.parentContainer = null;
		trayTransform.SetPosition(TrayWorldPosition);

		EjectItems();
		EjectPlayers();

		SoundManager.PlayNetworkedAtPos("BinOpen", DrawerWorldPosition, Random.Range(0.8f, 1.2f), sourceObj: gameObject);
		drawerState = DrawerState.Open; // [SyncVar] will update sprites
	}

	protected virtual void CloseDrawer()
	{
		trayBehaviour.parentContainer = drawerPushPull;
		trayBehaviour.VisibleState = false;

		GatherItems();
		if (storePlayers) GatherPlayers();

		SoundManager.PlayNetworkedAtPos("BinClose", DrawerWorldPosition, Random.Range(0.8f, 1.2f), sourceObj: gameObject);
		drawerState = DrawerState.Shut; // [SyncVar] will update sprites
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
			if (item.TryGetComponent(out Pipe pipe) && pipe.anchored) continue;

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
