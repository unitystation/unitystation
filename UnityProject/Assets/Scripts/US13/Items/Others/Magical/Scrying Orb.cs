using System.Runtime.CompilerServices;
using Mirror;
using UnityEngine;
using US13.Core.Camera;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Utils;
using US13.Objects;
using US13.Systems.Inventory;
using US13.Tilemaps.Behaviours.Objects;
using Util;

public class ScryingOrb : NetworkBehaviour,  ICheckedInteractable<HandActivate>, IItemInOutMovedPlayer, IClientSynchronisedEffect
{

	[field: SyncVar(hook = nameof(SyncOnPlayer))]
	public uint OnPlayerID { get; set; }

	public RegisterPlayer CurrentlyOn { get; set; }
	public bool PreviousSetValid { get; set; }
	private IClientSynchronisedEffect Preimplemented => (IClientSynchronisedEffect) this;


	public bool IsValidSetup(RegisterPlayer player)
	{
		if (player == null) return false;
		foreach (var itemSlot in player.PlayerScript.DynamicItemStorage.GetHandSlots())
		{
			if (itemSlot.ItemObject == gameObject)
			{
				return true;
			}
		}
		return false;
	}


	void IItemInOutMovedPlayer.ChangingPlayer(RegisterPlayer HideForPlayer, RegisterPlayer ShowForPlayer)
	{
		OnPlayerID = ShowForPlayer != null ? ShowForPlayer.netId : NetId.Empty;
	}


	public void SyncOnPlayer(uint PreviouslyOn, uint CurrentlyOn)
	{

		OnPlayerID = CurrentlyOn;
		Preimplemented.ImplementationSyncOnPlayer(PreviouslyOn, CurrentlyOn);
	}

	public void ApplyDefaultOrCurrentValues(bool Default)
	{
		ApplyChangesXray(Default ? false : true);
	}

	public void ApplyChangesXray(bool Value)
	{
		CameraEffectControlScript.Instance.Xray.RecordPosition(this.gameObject, Value);
	}


	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;

		return true;
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		interaction.PerformerPlayerScript.Mind.Ghost();
	}
}
