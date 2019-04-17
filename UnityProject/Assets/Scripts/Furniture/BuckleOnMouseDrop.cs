using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Buckle a player in when they are dragged and dropped while on this object.
/// </summary>
public class BuckleOnMouseDrop : CoordinatedInteraction<MouseDrop>
{
	protected override InteractionResult ServerPerformInteraction(MouseDrop drop)
	{
		var playerMove = drop.UsedObject.GetComponent<PlayerMove>();

		playerMove.Restrain();

		return InteractionResult.SOMETHING_HAPPENED;
	}

	protected override ValidationResult Validate(MouseDrop drop, NetworkSide side)
	{
		//validate the drop client side
		GameObject playerToBuckle = drop.UsedObject;
		//can only buckle things that are on this position.
		if (playerToBuckle.transform.position.CutToInt() != transform.position.CutToInt())
		{
			return ValidationResult.FAIL;
		}

		var playerMove = playerToBuckle.GetComponent<PlayerMove>();
		//can only buckle players
		if (playerMove == null)
		{
			return ValidationResult.FAIL;
		}

		return ValidationResult.SUCCESS;
	}
}
