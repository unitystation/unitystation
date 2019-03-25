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

    public void CleanTile (Vector3 spatsPos)
    {
	    Vector3Int targetWorldIntPos = spatsPos.CutToInt();
	    var floorDecals = MatrixManager.GetAt<FloorDecal>(targetWorldIntPos);
	    for ( var i = 0; i < floorDecals.Count; i++ )
	    {
		    floorDecals[i].DisappearFromWorldServer();
	    }

	    if (!MatrixManager.IsSpaceAt(targetWorldIntPos) )
	    {
		    EffectsFactory.Instance.WaterSplat(targetWorldIntPos);
	    }
    }
}