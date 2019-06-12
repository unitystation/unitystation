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

	protected override InteractionValidationChain<HandActivate> InteractionValidationChain()
	{
		return InteractionValidationChain<HandActivate>.EMPTY;
	}

	protected override void ServerPerformInteraction(HandActivate interaction)
	{
		startBuilding(interaction.Performer, interaction.Performer.transform.position);
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
		PoolManager.PoolNetworkInstantiate(girderPrefab, position);
		isBuilding = false;
		GetComponent<CustomNetTransform>().DisappearFromWorldServer();
	}

	[Server]
	private void CancelBuild()
	{
		isBuilding = false;
	}

}