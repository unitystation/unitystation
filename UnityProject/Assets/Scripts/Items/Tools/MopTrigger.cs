using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class MopTrigger : PickUpTrigger
{
	//server-side only, tracks if mop is currently cleaning.
	private bool isCleaning;

	public override bool Interact (GameObject originator, Vector3 position, string hand)
    {
		var playerScript = originator.GetComponent<PlayerScript>();
		//do nothing if player is not in reach of the specified position
		if (!playerScript.IsInReach(position, false))
		{
			return false;
		}

		if (PlayerManager.PlayerScript == playerScript)
		{
			//we are initiating the interaction locally

			//if the mop is not in our hand, pick it up
			if (UIManager.Hands.CurrentSlot.Item != gameObject)
			{
				return base.Interact (originator, position, hand);
			}

			//ask the server to let us mop if it's in reach
			if (PlayerManager.PlayerScript.IsInReach(position, false))
			{
				if (!isServer)
				{
					//ask to mop
					InteractMessage.Send(gameObject, position.RoundToInt(), hand);
				}
				else
				{
					//we're the server so we can just go ahead and mop
					ServerMop(originator, position);
				}

				return true;
			}
		}
		else if (isServer)
		{
			//server is being asked to mop on behalf of some other player.
			//if mop is not in their hand, pick it up by delegating to Pickuptrigger
			var targetSlot = InventoryManager.GetSlotFromOriginatorHand(originator, hand);
			if (targetSlot.Item == null)
			{
				return base.Interact (originator, position, hand);
			}

			//mop is in their hand, let them mop
			ServerMop(originator, position);
		}

		return false;
    }

	[Server]
	private void ServerMop(GameObject originator, Vector3 position)
	{
		if (!isCleaning)
		{
			//server is performing server-side logic for the interaction
			//do the mopping
			var progressFinishAction = new FinishProgressAction(
				reason =>
				{
					if (reason == FinishProgressAction.FinishReason.INTERRUPTED)
					{
						CancelCleanTile();
					}
					else if (reason == FinishProgressAction.FinishReason.COMPLETED)
					{
						CleanTile(position);
					}
				}
			);
			isCleaning = true;

			//Start the progress bar:
			UIManager.ProgressBar.StartProgress(position.RoundToInt(),
				5f, progressFinishAction, originator);
		}
	}


	[Server]
	private void CleanTile (Vector3 worldPos)
    {
	    var worldPosInt = worldPos.CutToInt();
	    var matrix = MatrixManager.AtPoint( worldPosInt, true );
	    var localPosInt = MatrixManager.WorldToLocalInt( worldPosInt, matrix );
	    var floorDecals = MatrixManager.GetAt<FloorDecal>(worldPosInt, isServer: true);

	    for ( var i = 0; i < floorDecals.Count; i++ )
	    {
		    floorDecals[i].DisappearFromWorldServer();
	    }

	    if (!MatrixManager.IsSpaceAt(worldPosInt, true))
	    {
		    // Create a WaterSplat Decal (visible slippery tile)
		    EffectsFactory.Instance.WaterSplat(worldPosInt);

		    // Sets a tile to slippery
		    matrix.MetaDataLayer.MakeSlipperyAt(localPosInt);
	    }

	    isCleaning = false;
    }

	[Server]
	private void CancelCleanTile()
	{
		//stop the in progress cleaning
		isCleaning = false;
	}
}