using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class MopTrigger : PickUpTrigger
{
	[SyncVar]
	private bool canBeUsed = true;

    public override bool Interact (GameObject originator, Vector3 position, string hand)
    {
        //TODO:  Fill this in.

        if (UIManager.Hands.CurrentSlot.Item != gameObject)
        {
            return base.Interact (originator, position, hand);
        }
        var targetWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		if (canBeUsed && PlayerManager.PlayerScript.IsInReach(targetWorldPos))
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
				UIManager.ProgressBar.StartProgress(targetWorldPos,
					10f, progressFinishAction, originator);
			}
			
        }

        return base.Interact (originator, position, hand);
    }

    //Broadcast from EquipmentPool.cs **ServerSide**
    public void OnAddToPool ()
    {
        canBeUsed = true;
    }

    //Broadcast from EquipmentPool.cs **ServerSide**
    public void OnRemoveFromInventory ()
    {
        canBeUsed = true;
    }

	public void CleanTile (Vector3 spatsPos)
	{
		Vector3Int targetWorldIntPos = spatsPos.CutToInt();
		IEnumerable bloodSpats = MatrixManager.GetAt<BloodSplat>(targetWorldIntPos);
		foreach (BloodSplat bloodSpat in bloodSpats)
		{
			bloodSpat.DisappearFromWorldServer();
		}
	}
}