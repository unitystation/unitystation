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
		GetComponent<Integrity>().OnWillDestroyServer.AddListener(OnWillDestroyServer);
	}

	private void OnWillDestroyServer(DestructionInfo arg0)
	{
		ObjectFactory.SpawnMetal(1, gameObject.TileWorldPosition(), parent: transform.parent);
	}

	protected override bool WillInteract(HandApply interaction, NetworkSide side)
	{
		//start with the default HandApply WillInteract logic.
		if (!base.WillInteract(interaction, side)) return false;

		//only care about interactions targeting us
		if (interaction.TargetObject != gameObject) return false;
		//only try to interact if the user has a wrench or metal in their hand
		if (!Validations.HasComponent<Metal>(interaction.HandObject) && !Validations.IsTool(interaction.HandObject, ToolType.Wrench)) return false;
		return true;
	}

	protected override void ServerPerformInteraction(HandApply interaction)
	{
		if (interaction.TargetObject != gameObject) return;

		if (Validations.HasComponent<Metal>(interaction.HandObject)){
			var progressFinishAction = new FinishProgressAction(
				reason =>
				{
					if (reason == FinishProgressAction.FinishReason.COMPLETED)
					{
						ConstructWall(interaction);
					}
				}
			);
			UIManager.ProgressBar.StartProgress(registerObject.WorldPositionServer, 5f, progressFinishAction, interaction.Performer);
		}
		else if (Validations.IsTool(interaction.HandObject, ToolType.Wrench))
		{
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
	private void ConstructWall(HandApply interaction){
		var handObj = interaction.HandObject;
		tileChangeManager.UpdateTile(Vector3Int.RoundToInt(transform.localPosition), TileType.Wall, "Wall");
		var slot = InventoryManager.GetSlotFromOriginatorHand(interaction.Performer, interaction.HandSlot.equipSlot);
		handObj.GetComponent<Pickupable>().DisappearObject(slot);
		GetComponent<CustomNetTransform>().DisappearFromWorldServer();
	}

}