using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

public class GirderTrigger : InputTrigger
{
	private TileChangeManager tileChangeManager;
    public GameObject metalPrefab;

	private void Start(){
        tileChangeManager = GetComponentInParent<TileChangeManager>();
	}

    public override bool Interact(GameObject originator, Vector3 position, string hand)
    {
        if (!CanUse(originator, hand, position, false))
        {
            return false;
        }
        if (!isServer)
        {
            return true;
        }

        PlayerNetworkActions pna = originator.GetComponent<PlayerNetworkActions>();
        GameObject handObj = pna.Inventory[hand].Item;
        if (handObj == null)
        {
            return false;
        }
        if (handObj.GetComponent<MetalTrigger>()){
			var progressFinishAction = new FinishProgressAction(
				reason =>
				{
					if (reason == FinishProgressAction.FinishReason.COMPLETED)
					{
						ConstructWall(handObj);
					}
				}
			);
			UIManager.ProgressBar.StartProgress(position.RoundToInt(), 5f, progressFinishAction, originator);
		}

		if (handObj.GetComponent<WrenchTrigger>()){
            SoundManager.PlayAtPosition("Wrench", transform.localPosition);
            var progressFinishAction = new FinishProgressAction(
                reason =>
                {
                    if (reason == FinishProgressAction.FinishReason.COMPLETED)
                    {
                        Disassemble();
                    }
                }
            );
            UIManager.ProgressBar.StartProgress(position.RoundToInt(), 5f, progressFinishAction, originator);
		}


        return true;
    }

    [Server]
    private void Disassemble()
    {
        PoolManager.PoolNetworkInstantiate(metalPrefab, transform.position);
        DisappearObject();
	}

	[Server]
	private void ConstructWall(GameObject handObj){
        tileChangeManager.UpdateTile(Vector3Int.RoundToInt(transform.localPosition), TileType.Wall, "Wall");
        handObj.GetComponent<PickUpTrigger>().DisappearObject();
        DisappearObject();
	}

}