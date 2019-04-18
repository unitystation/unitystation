using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class MetalTrigger : PickUpTrigger
{
    private bool isBuilding;
	public GameObject griderPrefab;


    public override void UI_Interact(GameObject originator, string hand)
    {
        base.UI_Interact(originator, hand);

        if (!isServer)
        {
            UIInteractMessage.Send(gameObject, UIManager.Hands.CurrentSlot.eventName);
        }
        else
        {
            startBuilding(originator, originator.transform.position);
        }
    }


    [Server]
    private void startBuilding(GameObject originator, Vector3 position)
    {
        if (!isBuilding)
        {
            var progressFinishAction = new FinishProgressAction(
                reason =>
                {
                    if (reason == FinishProgressAction.FinishReason.INTERRUPTED)
                    {
                        CancelBuild();
                    }
                    else if (reason == FinishProgressAction.FinishReason.COMPLETED)
                    {
                        BuildGirder(position);
                    }
                }
            );
            isBuilding = true;
            UIManager.ProgressBar.StartProgress(position.RoundToInt(), 5f, progressFinishAction, originator);
        }
    }

    [Server]
    private void BuildGirder(Vector3 position)
    {
        PoolManager.PoolNetworkInstantiate(griderPrefab, position);
        isBuilding = false;
        DisappearObject();
    }

    [Server]
    private void CancelBuild()
    {
        isBuilding = false;
    }
}