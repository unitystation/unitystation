using System.Collections;
using System.Linq;
using UnityEngine;
using Mirror;

/// <summary>
/// The main metal sheet component
/// </summary>
public class Metal : NetworkBehaviour, IInteractable<HandActivate>
{
	public GameObject girderPrefab;

	public void ServerPerformInteraction(HandActivate interaction)
	{
		startBuilding(interaction);
	}

	[Server]
	private void startBuilding(HandActivate interaction)
	{
		var progressFinishAction = new ProgressCompleteAction(() => BuildGirder(interaction));
		UIManager.ServerStartProgress(ProgressAction.Construction, interaction.Performer.TileWorldPosition().To3Int(), 5f, progressFinishAction, interaction.Performer);
	}

	[Server]
	private void BuildGirder(HandActivate interaction)
	{
		PoolManager.PoolNetworkInstantiate(girderPrefab, interaction.Performer.TileWorldPosition().To3Int());
		Inventory.ServerDespawn(interaction.HandSlot);
	}

}