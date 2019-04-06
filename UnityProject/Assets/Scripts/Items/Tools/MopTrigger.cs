using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class MopTrigger : PickUpTrigger
{
	public override bool Interact (GameObject originator, Vector3 position, string hand)
    {
        //TODO:  Fill this in.

        if (UIManager.Hands.CurrentSlot.Item != gameObject)
        {
            return base.Interact (originator, position, hand);
        }
        var targetWorldPos = Camera.main.ScreenToWorldPoint(CommonInput.mousePosition);
		if (PlayerManager.PlayerScript.IsInReach(targetWorldPos))
        {
			if(!isServer)
			{
				InteractMessage.Send(gameObject, hand);
			} else
			{
				var progressFinishAction = new FinishProgressAction(
					FinishProgressAction.Action.CleanTile,
					targetWorldPos,
					this
				);

				//Start the progress bar:
				UIManager.ProgressBar.StartProgress(Vector3Int.RoundToInt(targetWorldPos),
					5f, progressFinishAction, originator);
			}

        }

        return base.Interact (originator, position, hand);
    }

    public void CleanTile (Vector3 worldPos)
    {
	    var worldPosInt = worldPos.CutToInt();
	    var matrix = MatrixManager.AtPoint( worldPosInt );
	    var localPosInt = MatrixManager.WorldToLocalInt( worldPosInt, matrix );
	    var floorDecals = MatrixManager.GetAt<FloorDecal>(worldPosInt);

	    for ( var i = 0; i < floorDecals.Count; i++ )
	    {
		    floorDecals[i].DisappearFromWorldServer();
	    }

	    if (!MatrixManager.IsSpaceAt(worldPosInt))
	    {
		    // Create a WaterSplat Decal (visible slippery tile)
		    // EffectsFactory.Instance.WaterSplat(targetWorldIntPos);

		    // Sets a tile to slippery
		    matrix.MetaDataLayer.MakeSlipperyAt(localPosInt);
	    }

    }
}