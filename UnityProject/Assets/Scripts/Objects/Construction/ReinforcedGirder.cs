using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// The main reinforced girder component
/// </summary>
public class ReinforcedGirder : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	private TileChangeManager tileChangeManager;

	private RegisterObject registerObject;
	private ObjectBehaviour objectBehaviour;

	private bool strutsUnsecured;

	private void Start(){
		tileChangeManager = GetComponentInParent<TileChangeManager>();
		registerObject = GetComponent<RegisterObject>();
		GetComponent<Integrity>().OnWillDestroyServer.AddListener(OnWillDestroyServer);
		objectBehaviour = GetComponent<ObjectBehaviour>();
	}

	private void OnWillDestroyServer(DestructionInfo arg0)
	{
		Spawn.ServerPrefab(CommonPrefabs.Instance.Plasteel, gameObject.TileWorldPosition().To3Int(), transform.parent, count: 1,
			scatterRadius: Spawn.DefaultScatterRadius, cancelIfImpassable: true);
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		//start with the default HandApply WillInteract logic.
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		//only care about interactions targeting us
		if (interaction.TargetObject != gameObject) return false;
		//only try to interact if the user has plasteel or screwdriver
		if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Plasteel) &&
		    !Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver)) return false;
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (interaction.TargetObject != gameObject) return;

		if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Plasteel))
		{
			if (!objectBehaviour.IsPushable)
			{
				var progressFinishAction = new ProgressCompleteAction(() =>
					ConstructReinforcedWall(interaction));
				Chat.AddActionMsgToChat(interaction.Performer, $"You start finalizing the reinforced wall...",
					$"{interaction.Performer.ExpensiveName()} starts finalizing the reinforced wall...");
				UIManager.ServerStartProgress(ProgressAction.Construction, registerObject.WorldPositionServer, 5f, progressFinishAction, interaction.Performer);
			}
		}
		else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver))
		{
			ProgressBar bar = null;
			if (!strutsUnsecured)
			{
				var progressFinishAction = new ProgressCompleteAction(() =>
				{
					Chat.AddActionMsgToChat(interaction.Performer, $"You unsecure the support struts.",
						$"{interaction.Performer.ExpensiveName()} unsecures the support struts.");
					strutsUnsecured = true;
				});
				Chat.AddActionMsgToChat(interaction.Performer, $"You start unsecuring the support struts...",
					$"{interaction.Performer.ExpensiveName()} starts unsecuring the support struts...");
				bar = UIManager.ServerStartProgress(ProgressAction.Construction, registerObject.WorldPositionServer, 4f, progressFinishAction, interaction.Performer);
			}
			else
			{
				var progressFinishAction = new ProgressCompleteAction(() =>
				{
					Chat.AddActionMsgToChat(interaction.Performer, $"You secure the support struts.",
						$"{interaction.Performer.ExpensiveName()} secure the support struts.");
					strutsUnsecured = true;
				});
				Chat.AddActionMsgToChat(interaction.Performer, $"You start securing the support struts...",
					$"{interaction.Performer.ExpensiveName()} starts securing the support struts...");
				bar = UIManager.ServerStartProgress(ProgressAction.Construction, registerObject.WorldPositionServer, 4f, progressFinishAction, interaction.Performer);
			}
			if (bar != null)
			{
				SoundManager.PlayNetworkedAtPos("screwdriver#", registerObject.WorldPositionServer, 1f);
			}
		}
	}

	[Server]
	private void ConstructReinforcedWall(HandApply interaction)
	{
		Chat.AddActionMsgToChat(interaction.Performer, "You fully reinforce the wall.",
			$"{interaction.Performer.ExpensiveName()} fully reinforces the wall.");
		tileChangeManager.UpdateTile(Vector3Int.RoundToInt(transform.localPosition), TileType.Wall, "ReinforcedWall");
		interaction.HandObject.GetComponent<Stackable>().ServerConsume(1);
		Despawn.ServerSingle(gameObject);
	}
}