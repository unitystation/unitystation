using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

/// <summary>
/// The main girder component
/// </summary>
[RequireComponent(typeof(Pickupable))]
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
			//ask server to perform the interaction
			InteractMessage.Send(gameObject, position, hand);
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

		var tool = handObj.GetComponent<Tool>();
		if (tool != null && tool.ToolType == ToolType.Wrench){
			SoundManager.PlayNetworkedAtPos("Wrench", transform.localPosition, 1f);
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
		handObj.GetComponent<Pickupable>().DisappearObject();
		DisappearObject();
	}

}