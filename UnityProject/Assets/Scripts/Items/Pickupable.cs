using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

/// <summary>
/// Allows object to be picked up and put into hand.
/// </summary>
public class Pickupable : NBHandApplyInteractable, IRightClickable
{
	/// <summary>
	/// Event fired after the object is picked up, on server only.
	/// </summary>
	[HideInInspector]
	public PickupSuccessEvent OnPickupServer = new PickupSuccessEvent();
	/// <summary>
	/// Fired when item is being dropped, on server side only.
	/// </summary>
	[HideInInspector]
	public UnityEvent OnDropServer = new UnityEvent();

	// make sure to call this in subclasses
	public void Start()
	{
		CheckSpriteOrder();
	}

	protected override InteractionValidationChain<HandApply> InteractionValidationChain()
	{
		return InteractionValidationChain<HandApply>.Create()
			.WithValidation(IsHand.EMPTY)
			//We are the ones being picked up
			.WithValidation(TargetIs.GameObject(gameObject))
			//allow extended range, so we can nudge it if it is stationary and a little too far,
			//or allow picking it up if it is floating and a little too far
			.WithValidation(CanApply.EVEN_IF_SOFT_CRIT.WithRange(ReachRange.EXTENDED_SERVER));

	}

	protected override void ClientPredictInteraction(HandApply interaction)
	{
		if ( interaction.Performer.GetComponent<PlayerScript>().IsInReach( this.gameObject, false ))
		{
			//Predictive disappear only if item is within normal range
			gameObject.GetComponent<CustomNetTransform>().DisappearFromWorld();
		}
	}

	protected override void OnServerInteractionValidationFail(HandApply interaction)
	{
		//Rollback prediction (inform player about item's true state)
		GetComponent<CustomNetTransform>().NotifyPlayer(interaction.Performer);
	}


	protected override void ServerPerformInteraction(HandApply interaction)
	{
		//we validated, but object may only be in extended range
		var cnt = GetComponent<CustomNetTransform>();
		var ps = interaction.Performer.GetComponent<PlayerScript>();
		var extendedRangeOnly = !ps.IsInReach(cnt.RegisterTile, true);

		//if it's in extended range only, then we will nudge it if it is stationary
		//or pick it up if it is floating.
		if (extendedRangeOnly && !cnt.IsFloatingServer)
		{
			//this item is not floating and it was not within standard range but is within extended range,
			//so we will nudge it
			var worldPosition = cnt.RegisterTile.WorldPositionServer;
			var trajectory = ((Vector3)ps.WorldPos-worldPosition)/ Random.Range( 10, 31 );
			cnt.Nudge( new NudgeInfo
			{
				OriginPos = worldPosition - trajectory,
				Trajectory = trajectory,
				SpinMode = SpinMode.Clockwise,
				SpinMultiplier = 15,
				InitialSpeed = 2
			} );
			Logger.LogTraceFormat( "Nudging! server pos:{0} player pos:{1}", Category.Security,
				cnt.ServerState.WorldPosition, interaction.Performer.transform.position);
			//client prediction doesn't handle nudging, so we need to roll them back
			OnServerInteractionValidationFail(interaction);
		}
		else
		{
			//pick it up normally - if it was floating, we will grab it while it's floating
			//set ForceInform to false for simulation
			//the return value of AddItemToUISlot is an extra layer of validation on top of our other validations,
			//not sure if this is redundant with our validation chain.
			if (ps.playerNetworkActions.AddItemToUISlot(gameObject, interaction.HandSlot.SlotName, false /*, false*/))
			{
				Logger.LogTraceFormat("Pickup success! server pos:{0} player pos:{1} (floating={2})", Category.Security,
					cnt.ServerState.WorldPosition, interaction.Performer.transform.position, cnt.IsFloatingServer);
				//continue to what happens after pickup
				OnPickupServer.Invoke(interaction);
			}
			else
			{
				//for some reason pickup failed even after earlier validation, need to rollback client
				OnServerInteractionValidationFail(interaction);
			}
		}
	}

	public RightClickableResult GenerateRightClickOptions()
	{
		//would the interaction validate locally?
		var valid = InteractionValidationChain()
			//unlike the normal validation chain, we only want to show the option if they are within standard (not extended) range
			.WithValidation(CanApply.EVEN_IF_SOFT_CRIT.WithRange(ReachRange.STANDARD))
			.Validate(HandApply.ByLocalPlayer(gameObject), NetworkSide.CLIENT);


		if (valid == ValidationResult.SUCCESS)
		{
			return RightClickableResult.Create()
				.AddElement("PickUp", RightClickInteract);
		}

		return null;
	}

	private void RightClickInteract()
	{
		//trigger the interaction manually, triggered via the right click menu
		Interact(HandApply.ByLocalPlayer(gameObject));
	}

	//Broadcast from InventoryManager on server
	[Server]
	public void OnRemoveFromInventory()
	{
		OnDropServer.Invoke();
	}

	/// <summary>
	///     Making reach check less strict when object is flying, otherwise high ping players can never catch shit!
	/// </summary>
	private static bool CanReachFloating(PlayerScript ps, TransformState state)
	{
		return ps.IsInReach(state.WorldPosition, true) || ps.IsInReach(state.WorldPosition - (Vector3)state.Impulse, true, 1.75f);
	}

	/// <summary>
	///     If a SpriteRenderer.sortingOrder is 0 then there will be difficulty
	///     interacting with the object via the InputTrigger especially when placed on
	///     tables. This method makes sure that it is never 0 on start
	/// </summary>
	private void CheckSpriteOrder()
	{
		SpriteRenderer sR = GetComponentInChildren<SpriteRenderer>();
		if (sR != null)
		{
			if (sR.sortingLayerName == "Items" && sR.sortingOrder == 0)
			{
				sR.sortingOrder = 1;
			}
		}
	}

	/// <summary>
	// Removes the object from the player inventory before vanishing it
	/// </summary>
	[Server]
	public void DisappearObject()
	{
		InventoryManager.DestroyItemInSlot(gameObject);
		GetComponent<CustomNetTransform>().DisappearFromWorldServer();
	}
}

/// <summary>
/// Event which fires after a pickup occurs. Provides the interaction that was used to trigger the pickup
/// </summary>
public class PickupSuccessEvent : UnityEvent<HandApply>
{

}
