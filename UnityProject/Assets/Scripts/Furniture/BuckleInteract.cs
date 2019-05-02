using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Buckle a player in when they are dragged and dropped while on this object, then unbuckle
/// them when the object is hand-applied to.
/// </summary>
public class BuckleInteract : CoordinatedInteraction<MouseDrop, HandApply>
{
	//may be null
	private DirectionalSprite directionalSprite;

	private void Start()
	{
		directionalSprite = GetComponent<DirectionalSprite>();
		base.Start();
	}

	protected override IList<IInteractionValidator<MouseDrop>> Validators()
	{
		return new List<IInteractionValidator<MouseDrop>>
		{
			IsDroppedObjectAtTargetPosition.IS,
			DoesDroppedObjectHaveComponent<PlayerMove>.DOES,
			CanApply.EVEN_IF_SOFT_CRIT,
			ComponentAtTargetMatrixPosition<PlayerMove>.NoneMatchingCriteria(pm => pm.IsRestrained),
			new FunctionValidator<MouseDrop>(AdditionalValidation)
		};
	}

	private ValidationResult AdditionalValidation(MouseDrop drop, NetworkSide side)
	{
		//if the player to buckle is currently downed, we cannot buckle if there is another player on the tile
		//(because buckling a player causes the tile to become unpassable, thus a player could end up
		//occupying another player's space)
		var playerMove = drop.UsedObject.GetComponent<PlayerMove>();
		var registerPlayer = playerMove.GetComponent<RegisterPlayer>();
		if (side == NetworkSide.SERVER ? !registerPlayer.IsDownServer : !registerPlayer.IsDownClient) return ValidationResult.SUCCESS;
		return ComponentAtTargetMatrixPosition<PlayerMove>.NoneMatchingCriteria(pm =>
			pm != playerMove &&
			(side == NetworkSide.SERVER ? pm.GetComponent<RegisterPlayer>().IsBlockingServer
										: pm.GetComponent<RegisterPlayer>().IsBlockingClient))
			.Validate(drop, side);
	}

	protected override InteractionResult ServerPerformInteraction(MouseDrop drop)
	{
		var playerMove = drop.UsedObject.GetComponent<PlayerMove>();

		playerMove.Restrain(OnUnbuckle);

		//if this is a directional sprite, we render it in front of the player
		//when they are buckled
		directionalSprite?.RenderBuckledOverPlayerWhenUp(true);

		return InteractionResult.SOMETHING_HAPPENED;
	}

	protected override IList<IInteractionValidator<HandApply>> ValidatorsT2()
	{
		return new List<IInteractionValidator<HandApply>>
		{
			IsHand.EMPTY,
			TargetIs.GameObject(gameObject),
			CanApply.EVEN_IF_SOFT_CRIT,
			ComponentAtTargetMatrixPosition<PlayerMove>.MatchingCriteria(pm => pm.IsRestrained)
		};
	}

	protected override InteractionResult ServerPerformInteraction(HandApply interaction)
	{
		var playerMoveAtPosition = MatrixManager.GetAt<PlayerMove>(transform.position.CutToInt(), true)?.First(pm => pm.IsRestrained);
		//cannot use the CmdUnrestrain because commands are only allowed to be invoked by local player
		playerMoveAtPosition.Unrestrain();
		//the above will then invoke onunbuckle as it was the callback passed to Restrain

		return InteractionResult.SOMETHING_HAPPENED;
	}


	//delegate invoked from playerMove when they are unrestrained from this
	private void OnUnbuckle()
	{
		directionalSprite?.RenderBuckledOverPlayerWhenUp(false);
	}
}
