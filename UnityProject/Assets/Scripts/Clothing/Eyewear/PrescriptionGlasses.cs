using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PrescriptionGlasses : NetworkBehaviour,  IItemInOutMovedPlayer, IClientSynchronisedEffect
{
	private Pickupable pickupable;

	private void Awake()
	{
		pickupable =  GetComponent<Pickupable>();
	}

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

	private IClientSynchronisedEffect Preimplemented => (IClientSynchronisedEffect) this;

	[SyncVar(hook = nameof(SyncOnPlayer))] public uint OnBodyID;

	public uint OnPlayerID => OnBodyID;



	public void SyncOnPlayer(uint PreviouslyOn, uint CurrentlyOn)
	{
		OnBodyID = CurrentlyOn;
		Preimplemented.ImplementationSyncOnPlayer(PreviouslyOn, CurrentlyOn);
	}

	public void ApplyDefaultOrCurrentValues(bool Default)
	{
		ApplyEffects(Default ? false : true);
	}

	public void ApplyEffects(bool State)
	{
		Camera.main.GetComponent<CameraEffects.CameraEffectControlScript>().blurryVisionEffect.SetHasEyeCorrection(State);

	}
}
