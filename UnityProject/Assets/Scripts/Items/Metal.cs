using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Mirror;

/// <summary>
/// The main metal sheet component
/// </summary>
[RequireComponent(typeof(Stackable))]
public class Metal : MonoBehaviour, IInteractable<HandActivate>
{
	public GameObject girderPrefab;
	private Stackable stackable;

	private void Awake()
	{
		stackable = GetComponent<Stackable>();
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		ServerStartBuilding(interaction);
	}

	private void ServerStartBuilding(HandActivate interaction)
	{
		var progressFinishAction = new ProgressCompleteAction(() => ServerBuildGirder(interaction));
		UIManager.ServerStartProgress(ProgressAction.Construction, interaction.Performer.TileWorldPosition().To3Int(), 5f, progressFinishAction, interaction.Performer);
	}

	private void ServerBuildGirder(HandActivate interaction)
	{
		Spawn.ServerPrefab(girderPrefab, interaction.Performer.TileWorldPosition().To3Int());
		stackable.ServerConsume(1);
	}

}