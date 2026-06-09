using System;
using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Utils;
using US13.HealthV2.Living.Mutations.Surface;
using US13.Items;
using US13.Items.Weapons;
using US13.Managers.NetworkManagement;
using US13.Systems.Inventory;
using US13.Tilemaps.Behaviours.Objects;
using Util;

public class ChameleonProjector : MonoBehaviour, ICheckedInteractable<PositionalHandApply>,IItemInOutMovedPlayer
{

	public GameObject Targeted;

	//hummmmmm,
	//Option one have storage and make item appear on top (buggyyyyyyyyyyy)
	//Option 2, Generates sprites  hummmmm, how to network
	//Option 3, Make a handler to do it for you? static or instance? humm,
	//static Hard track and what about if something is destroyed
	//You need some way to remove as well when it's destroyed, hummmmmmmmm, technically would unregistered on destroy, Part of the generic handlers of sprite handler,
	//so that works, nope, relog, oh yeah rip
	//Now fut are narrow by
	//keeping updated with the state of the object, (so, How does a static manager know if something is been destroyed that it's trying to update sprites for, hummm)
	//Swapping out with a different object easily
	//RootBodyPartController Has similar function to but is designed for prefab sprites ( is in Eastbourne in a prefab that has all the Sprite handlers , and have multiple of those for custom effects )
	//so, Track names , and that's about it

	public RegisterPlayer CurrentlyOn { get; set; }
	public bool PreviousSetValid { get; set; }

	public Pickupable pickupable;

	public ActivatableWeapon ActivatableWeapon;

	public void Awake()
	{
		pickupable  = GetComponent<Pickupable>();
		ActivatableWeapon = this.GetComponent<ActivatableWeapon>();
		ActivatableWeapon.ServerOnActivate += CheckState;
		ActivatableWeapon.ServerOnDeactivate +=  CheckState;
	}

	public bool IsValidSetup(RegisterPlayer player)
	{
		if (player == null) return false;
		if (player != null && pickupable.ItemSlot?.Player == player)
		{
			return pickupable.ItemSlot.NamedSlot == NamedSlot.rightHand ||
			       pickupable.ItemSlot.NamedSlot == NamedSlot.rightHand;
			//Only turn on goggle for client if they are on
		}

		return false;
	}


	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;

		if (interaction.TargetObject == this.gameObject) return false;

		return true;
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		if (interaction.TargetObject.GetComponent<Attributes>() == null) return;
		Targeted = interaction.TargetObject;
		Chat.AddExamineMsgFromServer(interaction.Performer,$"You set {interaction.TargetObject.ExpensiveName()} on the {gameObject.ExpensiveName()}");
	}

	public void CheckState(GameObject GameObject)
	{
		if (CurrentlyOn != null)
		{
			if (ActivatableWeapon.IsActive == false)
			{
				var Visibility = CurrentlyOn.GetCachedComponent<BodySpritesInvisbility>();
				var Projection = CurrentlyOn.GetComponent<SpriteHandlerItemReplicatorNet>();
				Visibility.IncludeClothes = true;
				Visibility.Alpha = 0;
				Projection.TrackItem(Targeted);
			}
			else
			{
				var Visibility = CurrentlyOn.GetCachedComponent<BodySpritesInvisbility>();
				var Projection = CurrentlyOn.GetComponent<SpriteHandlerItemReplicatorNet>();
				Visibility.Alpha = 1;
				Visibility.IncludeClothes = false;
				Projection.TrackItem(null);
			}

		}
	}


	public void ChangingPlayer(RegisterPlayer HideForPlayer, RegisterPlayer ShowForPlayer)
	{
		if (HideForPlayer != null)
		{
			var Visibility = HideForPlayer.GetCachedComponent<BodySpritesInvisbility>();
			var Projection = HideForPlayer.GetComponent<SpriteHandlerItemReplicatorNet>();
			Visibility.Alpha = 1;
			Visibility.IncludeClothes = false;
			Projection.TrackItem(null);
		}

		if (ShowForPlayer != null)
		{
			if (ActivatableWeapon.IsActive && Targeted != null)
			{
				var Visibility = ShowForPlayer.GetCachedComponent<BodySpritesInvisbility>();
				var Projection = ShowForPlayer.GetComponent<SpriteHandlerItemReplicatorNet>();
				Visibility.IncludeClothes = true;
				Visibility.Alpha = 0;
				Projection.TrackItem(Targeted);
			}
		}
	}


}
