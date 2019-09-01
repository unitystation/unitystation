using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// The main metal sheet component
/// </summary>
public class Metal : NBHandActivateInteractable
{
	private bool isBuilding;
	public GameObject girderPrefab;

	protected override void ServerPerformInteraction(HandActivate interaction)
	{
		startBuilding(interaction);
	}

	[Server]
	private void startBuilding(HandActivate interaction)
	{
		if (!isBuilding)
		{
			var position = interaction.Performer.transform.position;
			var progressFinishAction = new FinishProgressAction(
				reason =>
				{
					if (reason == FinishProgressAction.FinishReason.INTERRUPTED)
					{
						CancelBuild();
					}
					else if (reason == FinishProgressAction.FinishReason.COMPLETED)
					{
						BuildGirder(interaction, position);
					}
				}
			);
			isBuilding = true;
			UIManager.ProgressBar.StartProgress(position.RoundToInt(), 5f, progressFinishAction, interaction.Performer);
		}
	}

	[Server]
	private void BuildGirder(HandActivate interaction, Vector3 position)
	{
		PoolManager.PoolNetworkInstantiate(girderPrefab, position);
		isBuilding = false;
		var slot = InventoryManager.GetSlotFromOriginatorHand(interaction.Performer, interaction.HandSlot.equipSlot);
		GetComponent<Pickupable>().DisappearObject(slot);
		GetComponent<CustomNetTransform>().DisappearFromWorldServer();

	}

	[Server]
	private void CancelBuild()
	{
		isBuilding = false;
	}

}