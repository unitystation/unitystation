using UnityEngine;
using Mirror;
using Player;
using UI.Action;

public class NightVisionGoggles : NetworkBehaviour, IItemInOutMovedPlayer,
	ICheckedInteractable<HandActivate>
{

	private NightVisionData VisionData = new NightVisionData(b:true);

	[SerializeField, Tooltip("How far the player will be able to see in the dark while he has the goggles on.")]
	private Vector3 nightVisionVisibility { get; set; }

	[System.Serializable]
	public struct NightVisionData
	{
		public bool isOn  { get; set; }


		[SerializeField, Tooltip("How far the player will be able to see in the dark while he has the goggles on.")]
		public Vector3 nightVisionVisibility { get; set; }

		[SerializeField, Tooltip("How fast will the player gain visibility?")]
		public float visibilityAnimationSpeed { get; set; }

		public NightVisionData(bool b)
		{
			isOn = false;
			nightVisionVisibility = new Vector3(10.5f, 10.5f, 21);
			visibilityAnimationSpeed = 1.5f;
		}
	}


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

	public RegisterPlayer CurrentlyOn { get; set; }
	bool IItemInOutMovedPlayer.PreviousSetValid { get; set; }

	public bool IsValidSetup(RegisterPlayer player)
	{
		if (player == null) return false;
		if (player != null && player.PlayerScript.RegisterPlayer == pickupable.ItemSlot.Player && pickupable.ItemSlot is {NamedSlot: NamedSlot.eyes}) // Checks if it's not null and checks if NamedSlot == NamedSlot.eyes
		{
			return true;
		}

		return false;
	}


	void IItemInOutMovedPlayer.ChangingPlayer(RegisterPlayer HideForPlayer, RegisterPlayer ShowForPlayer)
	{
		if (HideForPlayer != null)
		{
			ServerToggleClient(HideForPlayer, new NightVisionData(b:true));
		}

		if (ShowForPlayer != null)
		{
			ServerToggleClient(ShowForPlayer, VisionData);
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
		VisionData.isOn = !VisionData.isOn;
		Chat.AddExamineMsgToClient($"You turned {(VisionData.isOn ? "on" : "off")} the {gameObject.ExpensiveName()}.");
	}

	#endregion

	[Server]
	private void ToggleGoggles()
	{
		SetGoggleState(!VisionData.isOn);
	}

	/// <summary>
	/// Turning goggles on or off
	/// </summary>
	/// <param name="newState"></param>
	[Server]
	private void SetGoggleState(bool newState)
	{

		VisionData.isOn = newState;
		if (CurrentlyOn == null || CurrentlyOn.PlayerScript.connectionToClient == null) return;

		if (IsValidSetup(CurrentlyOn))
		{
			ServerToggleClient(CurrentlyOn,VisionData);

			Chat.AddExamineMsgFromServer(CurrentlyOn.PlayerScript.gameObject,
				$"You turned {(VisionData.isOn ? "on" : "off")} the {gameObject.ExpensiveName()}.");
		}
	}

	[Server]
	private void ServerToggleClient(RegisterPlayer forPlayer, NightVisionData newState)
	{
		forPlayer.PlayerScript.PlayerOnlySyncValues.SyncNightVision(new NightVisionData(b:true), newState);
	}
}