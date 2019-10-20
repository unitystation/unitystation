using System.Collections;
using System.Linq;
using UnityEngine;
using Mirror;

/// <summary>
/// The main metal sheet component
/// </summary>
public class Metal : NBHandActivateInteractable
{
	public GameObject girderPrefab;

	protected override void ServerPerformInteraction(HandActivate interaction)
	{
		startBuilding(interaction);
	}

	[Server]
	private void startBuilding(HandActivate interaction)
	{
		var progressFinishAction = new FinishProgressAction(
			reason =>
			{
				if (reason == FinishReason.COMPLETED)
				{
					BuildGirder(interaction);
				}
			}
		);
		UIManager.ServerStartProgress(interaction.Performer.TileWorldPosition().To3Int(), 5f, progressFinishAction, interaction.Performer, true);
	}

	[Server]
	private void BuildGirder(HandActivate interaction)
	{
		PoolManager.PoolNetworkInstantiate(girderPrefab, interaction.Performer.TileWorldPosition().To3Int());
		var slot = InventoryManager.GetSlotFromOriginatorHand(interaction.Performer, interaction.HandSlot.equipSlot);
		GetComponent<Pickupable>().DisappearObject(slot);
		GetComponent<CustomNetTransform>().DisappearFromWorldServer();

	}

}