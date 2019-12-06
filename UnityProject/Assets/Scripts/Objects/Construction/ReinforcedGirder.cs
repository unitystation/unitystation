using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// The main reinforced girder component
/// </summary>
public class ReinforcedGirder : NetworkBehaviour, ICheckedInteractable<HandApply>, IServerSpawn
{
	private TileChangeManager tileChangeManager;

	private RegisterObject registerObject;
	private ObjectBehaviour objectBehaviour;

	private bool strutsUnsecured;

	[Tooltip("Normal girder prefab.")]
	[SerializeField]
	private GameObject girder;

	[Tooltip("Tile to spawn when wall is constructed.")]
	[SerializeField]
	private BasicTile reinforcedWallTile;

	private void Start(){
		tileChangeManager = GetComponentInParent<TileChangeManager>();
		registerObject = GetComponent<RegisterObject>();
		GetComponent<Integrity>().OnWillDestroyServer.AddListener(OnWillDestroyServer);
		objectBehaviour = GetComponent<ObjectBehaviour>();
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		strutsUnsecured = false;
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
		//only try to interact if the user has plasteel, screwdriver, or wirecutter
		if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Plasteel) &&
		    !Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver) &&
		    !Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wirecutter)) return false;
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (interaction.TargetObject != gameObject) return;

		if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Plasteel))
		{
			if (!strutsUnsecured)
			{
				var progressFinishAction = new ProgressCompleteAction(() =>
					ConstructReinforcedWall(interaction));
				Chat.AddActionMsgToChat(interaction.Performer, $"You start finalizing the reinforced wall...",
					$"{interaction.Performer.ExpensiveName()} starts finalizing the reinforced wall...");
				ToolUtils.ServerUseTool(interaction, 5f, progressFinishAction);
			}
		}
		else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver))
		{
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
				ToolUtils.ServerUseTool(interaction, 4f, progressFinishAction);
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
				ToolUtils.ServerUseTool(interaction, 4f, progressFinishAction);
			}
		}
		else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wirecutter))
		{
			if (strutsUnsecured)
			{
				var progressFinishAction = new ProgressCompleteAction(() =>
				{
					Chat.AddActionMsgToChat(interaction.Performer, $"You remove the inner grille.",
						$"{interaction.Performer.ExpensiveName()} removes the inner grille.");
					Spawn.ServerPrefab(girder, SpawnDestination.At(gameObject));
					Spawn.ServerPrefab(CommonPrefabs.Instance.Plasteel, SpawnDestination.At(gameObject));
					Despawn.ServerSingle(gameObject);
				});
				Chat.AddActionMsgToChat(interaction.Performer, $"You start removing the inner grille...",
					$"{interaction.Performer.ExpensiveName()} starts removing the inner grille...");
				ToolUtils.ServerUseTool(interaction, 4f, progressFinishAction);
			}
		}
	}

	[Server]
	private void ConstructReinforcedWall(HandApply interaction)
	{
		Chat.AddActionMsgToChat(interaction.Performer, "You fully reinforce the wall.",
			$"{interaction.Performer.ExpensiveName()} fully reinforces the wall.");
		tileChangeManager.UpdateTile(Vector3Int.RoundToInt(transform.localPosition), reinforcedWallTile);
		interaction.HandObject.GetComponent<Stackable>().ServerConsume(1);
		Despawn.ServerSingle(gameObject);
	}
}