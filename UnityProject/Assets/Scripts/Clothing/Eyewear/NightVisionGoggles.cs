using UnityEngine;
using Mirror;
using Player;
using UI.Action;

public class NightVisionGoggles : NetworkBehaviour, IItemInOutMovedPlayer,
	ICheckedInteractable<HandActivate>, IClientSynchronisedEffect
{

	[SerializeField, Tooltip("How fast will the player gain visibility?")]
	private float visibilityAnimationSpeed = 1.50f;


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

		var loc = VisionData;
		loc.visibilityAnimationSpeed = visibilityAnimationSpeed;
		VisionData = loc;

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
		if (ShowForPlayer != null)
		{
			OnBodyID = ShowForPlayer.netId;
		}
		else
		{
			OnBodyID = NetId.Empty;
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
		var Data = VisionData;
		Data.isOn = !VisionData.isOn;
		VisionData = Data;
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

		var Data = VisionData;
		Data.isOn = newState;
		VisionData = Data;

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
		VisionData = newState;
	}

	private IClientSynchronisedEffect Preimplemented => (IClientSynchronisedEffect) this;

	[SyncVar(hook = nameof(SyncOnPlayer))] public uint OnBodyID;

	public uint OnPlayerID => OnBodyID;

	[SyncVar(hook = nameof(SyncNightVision))]
	private NightVisionData VisionData = new NightVisionData(b:true);
	private NightVisionData DefaultVisionData = new NightVisionData(b:true)
	{
		isOn = false
	};


	public void SyncOnPlayer(uint PreviouslyOn, uint CurrentlyOn)
	{
		OnBodyID = CurrentlyOn;
		Preimplemented.ImplementationSyncOnPlayer(PreviouslyOn, CurrentlyOn);
	}

	public void ApplyDefaultOrCurrentValues(bool Default)
	{
		ApplyEffects(Default ? DefaultVisionData : VisionData);
	}

	public void SyncNightVision(NightVisionGoggles.NightVisionData oldState, NightVisionGoggles.NightVisionData newState)
	{
		VisionData = newState;

		if (Preimplemented.IsOnLocalPlayer)
		{
			ApplyEffects(VisionData);
		}
	}

	public void ApplyEffects(NightVisionData State)
	{
		Camera.main.GetComponent<CameraEffects.CameraEffectControlScript>().AdjustPlayerVisibility(State.nightVisionVisibility, State.isOn ? State.visibilityAnimationSpeed : 0.1f);
		Camera.main.GetComponent<CameraEffects.CameraEffectControlScript>().ToggleNightVisionEffectState(State.isOn);
	}
}