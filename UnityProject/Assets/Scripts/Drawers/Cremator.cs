using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cremator component for cremator objects, for use in crematorium rooms. Adds additional function to the base Drawer component.
/// TODO: Implement activation via button when buttons can be assigned a generic component instead of only a DoorController component
/// and remove the activation by right-click option.
/// </summary>
public class Cremator : Drawer, IRightClickable, ICheckedInteractable<MouseDrop>
{
	// Extra states over the base DrawerState enum.
	private enum CrematorState
	{
		/// <summary> Red light in red display. </summary>
		ShutWithContents = 2,
		/// <summary> Cremator is cremating. </summary>
		ShutAndActive = 3,
	}

	private AccessRestrictions accessRestrictions;
	private OverlayTile ashOverlay;

	private const float BURNING_DURATION = 1.5f; // In seconds - timed to the Ding SFX.

	private bool containsAsh = false;

	protected override void Awake()
	{
		base.Awake();
		accessRestrictions = GetComponent<AccessRestrictions>();
		ashOverlay = TileManager.GetTile(TileType.Effects, "SmallAsh") as OverlayTile;
	}

	// This region (Interaction-RightClick) shouldn't exist once TODO in class summary is done.
	#region Interaction-RightClick

	public RightClickableResult GenerateRightClickOptions()
	{
		RightClickableResult result = RightClickableResult.Create();
		if (drawerState == DrawerState.Open) return result;
		if (!accessRestrictions.CheckAccess(PlayerManager.LocalPlayer)) return result;

		return result.AddElement("Activate Ignition", RightClickInteract);
	}

	private void RightClickInteract()
	{
		InteractionUtils.RequestInteract(MouseDrop.ByLocalPlayer(gameObject, PlayerManager.LocalPlayer), this);
	}

	public bool WillInteract(MouseDrop interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (drawerState == (DrawerState)CrematorState.ShutAndActive) return false;

		return true;
	}

	public void ServerPerformInteraction(MouseDrop interaction)
	{
		Cremate();
	}

	#endregion Interaction-RightClick

	#region Interaction

	public override void ServerPerformInteraction(HandApply interaction)
	{
		if (drawerState == (DrawerState)CrematorState.ShutAndActive) return;
		base.ServerPerformInteraction(interaction);
	}

	#endregion Interaction

	#region Server Only

	protected override void OpenDrawer()
	{
		base.OpenDrawer();
		if (containsAsh)
		{
			registerObject.TileChangeManager.UpdateOverlay(TrayLocalPosition, ashOverlay);
			containsAsh = false;
		}
	}

	protected override void CloseDrawer()
	{
		base.CloseDrawer();
		// Note: the sprite setting done in base.CloseDrawer() would be overridden (an unnecessary sprite call).
		// "Not great, not terrible."

		UpdateCloseState();
	}

	private void UpdateCloseState()
	{
		if (serverHeldItems.Count > 0 || serverHeldPlayers.Count > 0)
		{
			drawerState = (DrawerState)CrematorState.ShutWithContents;
		}
		else drawerState = DrawerState.Shut;
	}

	private void Cremate()
	{
		OnStartPlayerCremation();
		StartCoroutine(PlayIncineratingAnim());
		SoundManager.PlayNetworkedAtPos("Ding", DrawerWorldPosition, sourceObj: gameObject);
		DestroyItems();

		if (serverHeldItems.Count > 0 || serverHeldPlayers.Count > 0) containsAsh = true;		
	}

	private void DestroyItems()
	{
		foreach (KeyValuePair<ObjectBehaviour, Vector3> item in serverHeldItems)
		{
			Despawn.ServerSingle(item.Key.gameObject);
		}

		serverHeldItems = new Dictionary<ObjectBehaviour, Vector3>();
	}

	private void OnStartPlayerCremation()
	{
		var containsConsciousPlayer = false;

		foreach (ObjectBehaviour player in serverHeldPlayers)
		{
			LivingHealthBehaviour playerLHB = player.GetComponent<LivingHealthBehaviour>();
			if (playerLHB.ConsciousState == ConsciousState.CONSCIOUS ||
				playerLHB.ConsciousState == ConsciousState.BARELY_CONSCIOUS)
			{
				containsConsciousPlayer = true;
			}
		}

		if (containsConsciousPlayer)
		{
			// This is an incredibly brutal SFX... it also needs chopping up.
			// SoundManager.PlayNetworkedAtPos("ShyguyScream", DrawerWorldPosition, sourceObj: gameObject);
		}
	}

	private void OnFinishPlayerCremation()
	{
		foreach (var player in serverHeldPlayers)
		{
			var playerScript = player.GetComponent<PlayerScript>();
			PlayerSpawn.ServerSpawnGhost(playerScript.mind);
			Despawn.ServerSingle(player.gameObject);
		}

		serverHeldPlayers = new List<ObjectBehaviour>();
	}

	private IEnumerator PlayIncineratingAnim()
	{
		drawerState = (DrawerState)CrematorState.ShutAndActive;
		yield return WaitFor.Seconds(BURNING_DURATION);
		OnFinishPlayerCremation();
		UpdateCloseState();
	}

	#endregion Server Only
}
