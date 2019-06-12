using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// The main girder component
/// </summary>
[RequireComponent(typeof(RegisterObject))]
[RequireComponent(typeof(Pickupable))]
public class Girder : NBHandApplyInteractable
{
	private TileChangeManager tileChangeManager;
	public GameObject metalPrefab;

	private RegisterObject registerObject;

	private void Start(){
		tileChangeManager = GetComponentInParent<TileChangeManager>();
		registerObject = GetComponent<RegisterObject>();
	}

	protected override InteractionValidationChain<HandApply> InteractionValidationChain()
	{
		return CommonValidationChains.CAN_APPLY_HAND_CONSCIOUS
			.WithValidation(TargetIs.GameObject(gameObject))
			.WithValidation(IsHand.OCCUPIED);
	}

	protected override void ServerPerformInteraction(HandApply interaction)
	{
		if (interaction.UsedObject.GetComponent<Metal>()){
			var progressFinishAction = new FinishProgressAction(
				reason =>
				{
					if (reason == FinishProgressAction.FinishReason.COMPLETED)
					{
						ConstructWall(interaction.UsedObject);
					}
				}
			);
			UIManager.ProgressBar.StartProgress(registerObject.WorldPositionServer, 5f, progressFinishAction, interaction.Performer);
		}

		var tool = interaction.UsedObject.GetComponent<Tool>();
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
			UIManager.ProgressBar.StartProgress(registerObject.WorldPositionServer, 5f, progressFinishAction, interaction.Performer);
		}
	}

	[Server]
	private void Disassemble()
	{
		PoolManager.PoolNetworkInstantiate(metalPrefab, registerObject.WorldPositionServer);
		GetComponent<CustomNetTransform>().DisappearFromWorldServer();
	}

	[Server]
	private void ConstructWall(GameObject handObj){
		tileChangeManager.UpdateTile(Vector3Int.RoundToInt(transform.localPosition), TileType.Wall, "Wall");
		handObj.GetComponent<Pickupable>().DisappearObject();
		GetComponent<CustomNetTransform>().DisappearFromWorldServer();
	}

}