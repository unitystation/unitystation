using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// The main girder component
/// </summary>
public class Girder : NetworkBehaviour, ICheckedInteractable<HandApply>, IServerSpawn
{
	private TileChangeManager tileChangeManager;

	private RegisterObject registerObject;
	private ObjectBehaviour objectBehaviour;

	[Tooltip("Reinforced girder prefab.")]
	[SerializeField]
	private GameObject reinforcedGirder;

	//tracked server side only
	private int plasteelSheetCount;

	private void Start(){
		tileChangeManager = GetComponentInParent<TileChangeManager>();
		registerObject = GetComponent<RegisterObject>();
		GetComponent<Integrity>().OnWillDestroyServer.AddListener(OnWillDestroyServer);
		objectBehaviour = GetComponent<ObjectBehaviour>();
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		plasteelSheetCount = 0;
	}

	private void OnWillDestroyServer(DestructionInfo arg0)
	{
		Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, gameObject.TileWorldPosition().To3Int(), transform.parent, count: 1,
			scatterRadius: Spawn.DefaultScatterRadius, cancelIfImpassable: true);
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		//start with the default HandApply WillInteract logic.
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		//only care about interactions targeting us
		if (interaction.TargetObject != gameObject) return false;
		//only try to interact if the user has a wrench, screwdriver, metal, or plasteel in their hand
		if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Metal) &&
			!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Plasteel) &&
		    !Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench) &&
		    !Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver)) return false;
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (interaction.TargetObject != gameObject) return;

		if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Metal))
		{
			//TODO: false walls
			if (objectBehaviour.IsPushable)
			{
				Chat.AddExamineMsg(interaction.Performer, "You've temporarily forgotten how to build false walls.");
			}
			else
			{
				if (!Validations.HasAtLeast(interaction.HandObject, 2))
				{
					Chat.AddExamineMsg(interaction.Performer, "You need two sheets of metal to finish a wall!");
					return;
				}

				var progressFinishAction = new ProgressCompleteAction(() =>
					ConstructWall(interaction));
				Chat.AddActionMsgToChat(interaction.Performer, $"You start adding plating...",
					$"{interaction.Performer.ExpensiveName()} begins adding plating...");
				UIManager.ServerStartProgress(ProgressAction.Construction, registerObject.WorldPositionServer, 4f, progressFinishAction, interaction.Performer);
			}
		}
		else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Plasteel))
		{
			//TODO: false reinforced walls
			if (objectBehaviour.IsPushable)
			{
				if (!Validations.HasAtLeast(interaction.HandObject, 2))
				{
					Chat.AddExamineMsg(interaction.Performer, "You need at least two sheets to create a false wall!");
					return;
				}
				Chat.AddExamineMsg(interaction.Performer, "You've temporarily forgotten how to build reinforced false walls.");
			}
			else
			{
				//add plasteel for constructing reinforced girder
				var progressFinishAction = new ProgressCompleteAction(() =>
					ReinforceGirder(interaction));
				Chat.AddActionMsgToChat(interaction.Performer, $"You start reinforcing the girder...",
					$"{interaction.Performer.ExpensiveName()} starts reinforcing the girder...");
				UIManager.ServerStartProgress(ProgressAction.Construction, registerObject.WorldPositionServer, 6f,
					progressFinishAction, interaction.Performer);
			}
		}
		else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench))
		{
			if (objectBehaviour.IsPushable)
			{
				//secure it if there's floor
				if (MatrixManager.IsSpaceAt(registerObject.WorldPositionServer, true))
				{
					Chat.AddExamineMsg(interaction.Performer, "A floor must be present to secure the girder!");
					return;
				}
				Chat.AddActionMsgToChat(interaction.Performer, $"You start securing the girder...",
					$"{interaction.Performer.ExpensiveName()} starts securing the girder...");
				var progressFinishAction = new ProgressCompleteAction(() =>
				{
					Chat.AddActionMsgToChat(interaction.Performer, $"You secure the girder.",
						$"{interaction.Performer.ExpensiveName()} secures the girder.");
					objectBehaviour.ServerSetPushable(false);
				});
				ToolUtils.ServerUseTool(interaction, 4f, progressFinishAction);
			}
			else
			{
				//unsecure it
				Chat.AddActionMsgToChat(interaction.Performer, $"You start unsecuring the girder...",
					$"{interaction.Performer.ExpensiveName()} starts unsecuring the girder...");
				var progressFinishAction = new ProgressCompleteAction(() =>
				{
					Chat.AddActionMsgToChat(interaction.Performer, $"You unsecure the girder.",
						$"{interaction.Performer.ExpensiveName()} unsecures the girder.");
					objectBehaviour.ServerSetPushable(true);
				});
				ToolUtils.ServerUseTool(interaction, 4f, progressFinishAction);
			}

		}
		else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver))
		{
			//disassemble if it's unanchored
			if (objectBehaviour.IsPushable)
			{
				Chat.AddActionMsgToChat(interaction.Performer, $"You start to disassemble the girder...",
					$"{interaction.Performer.ExpensiveName()} disassembles the girder.");
				var progressFinishAction = new ProgressCompleteAction(() => Disassemble(interaction));
				ToolUtils.ServerUseTool(interaction, 4f, progressFinishAction);
			}
			else
			{
				Chat.AddExamineMsg(interaction.Performer, "You must unsecure it first.");
			}
		}
	}

	[Server]
	private void Disassemble(HandApply interaction)
	{
		Chat.AddActionMsgToChat(interaction.Performer, "You disassemble the girder.",
			$"{interaction.Performer.ExpensiveName()} disassembles the girder.");
		Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, registerObject.WorldPositionServer, count: 2);
		GetComponent<CustomNetTransform>().DisappearFromWorldServer();
	}

	[Server]
	private void ConstructWall(HandApply interaction)
	{
		Chat.AddActionMsgToChat(interaction.Performer, "You add the plating.",
			$"{interaction.Performer.ExpensiveName()} adds the plating.");
		tileChangeManager.UpdateTile(Vector3Int.RoundToInt(transform.localPosition), TileType.Wall, "Wall");
		interaction.HandObject.GetComponent<Stackable>().ServerConsume(2);
		Despawn.ServerSingle(gameObject);
	}

	[Server]
	private void ReinforceGirder(HandApply interaction)
	{
		Chat.AddActionMsgToChat(interaction.Performer, "You reinforce the girder.",
			$"{interaction.Performer.ExpensiveName()} reinforces the girder.");
		interaction.HandObject.GetComponent<Stackable>().ServerConsume(1);
		Spawn.ServerPrefab(reinforcedGirder, SpawnDestination.At(gameObject));
		Despawn.ServerSingle(gameObject);
	}


}