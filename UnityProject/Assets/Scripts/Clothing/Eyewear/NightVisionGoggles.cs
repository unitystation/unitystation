using UnityEngine;
using Mirror;
using Player;
using UI.Action;

public class NightVisionGoggles : NetworkBehaviour, IItemInOutMovedPlayerButClientTracked,
	ICheckedInteractable<HandActivate>
{
	[SerializeField, Tooltip("How far the player will be able to see in the dark while he has the goggles on.")]
	private Vector3 nightVisionVisibility;

	[SerializeField, Tooltip("How fast will the player gain visibility?")]
	private float visibilityAnimationSpeed = 1.50f;

	private bool isOn;
	private ItemActionButton actionButton;
	private Pickupable pickupable;

	#region LifeCycle

	private void Awake()
	{
		actionButton = GetComponent<ItemActionButton>();
		pickupable =  GetComponent<Pickupable>();
	}

	private void OnEnable()
	{
		actionButton.ServerActionClicked += ToggleGoggles;
	}

	private void OnDisable()
	{
		actionButton.ServerActionClicked -= ToggleGoggles;
	}

	#endregion

	#region InventoryMove

	public Mind CurrentlyOn { get; set; }
	bool IItemInOutMovedPlayer.PreviousSetValid { get; set; }

	public bool IsValidSetup(Mind player)
	{
		if (player != null && player.CurrentPlayScript.RegisterPlayer == pickupable.ItemSlot.Player && pickupable.ItemSlot is {NamedSlot: NamedSlot.eyes}) // Checks if it's not null and checks if NamedSlot == NamedSlot.eyes
		{
			//Only turn on goggle for client if they are on
			return isOn;
		}

		return false;
	}


	void IItemInOutMovedPlayer.ChangingPlayer(Mind HideForPlayer, Mind ShowForPlayer)
	{
		if (HideForPlayer != null)
		{
			ServerToggleClient(HideForPlayer, false);
		}

		if (ShowForPlayer != null)
		{
			ServerToggleClient(ShowForPlayer, true);
		}
	}

	#endregion

	#region HandInteract

	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		return DefaultWillInteract.Default(interaction, side);
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		isOn = !isOn;
		Chat.AddExamineMsgToClient($"You turned {(isOn ? "on" : "off")} the {gameObject.ExpensiveName()}.");
	}

	#endregion

	[Server]
	private void ToggleGoggles()
	{
		SetGoggleState(!isOn);
	}

	/// <summary>
	/// Turning goggles on or off
	/// </summary>
	/// <param name="newState"></param>
	[Server]
	private void SetGoggleState(bool newState)
	{
		if (CurrentlyOn == null || CurrentlyOn.CurrentPlayScript.connectionToClient == null) return;

		isOn = newState;
		if (IsValidSetup(CurrentlyOn))
		{
			ServerToggleClient(CurrentlyOn,newState);
			Chat.AddExamineMsgFromServer(CurrentlyOn.CurrentPlayScript.gameObject,
				$"You turned {(isOn ? "on" : "off")} the {gameObject.ExpensiveName()}.");
		}
	}

	[Server]
	private void ServerToggleClient(Mind forPlayer, bool newState)
	{
		forPlayer.CurrentPlayScript.PlayerOnlySyncValues.ServerSetNightVision(newState, nightVisionVisibility,
			visibilityAnimationSpeed);
	}
}