using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// The main girder component
/// </summary>
[RequireComponent(typeof(RegisterObject))]
[RequireComponent(typeof(Pickupable))]
public class Girder : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	private TileChangeManager tileChangeManager;

	private RegisterObject registerObject;
	private ObjectBehaviour objectBehaviour;

	private void Start(){
		tileChangeManager = GetComponentInParent<TileChangeManager>();
		registerObject = GetComponent<RegisterObject>();
		GetComponent<Integrity>().OnWillDestroyServer.AddListener(OnWillDestroyServer);
		objectBehaviour = GetComponent<ObjectBehaviour>();
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
		//only try to interact if the user has a wrench, screwdriver, or metal in their hand
		if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Metal) &&
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
		else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench))
		{
			ProgressBar bar = null;
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
				bar = UIManager.ServerStartProgress(ProgressAction.Construction, registerObject.WorldPositionServer, 4f, progressFinishAction, interaction.Performer);
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
				bar = UIManager.ServerStartProgress(ProgressAction.Construction, registerObject.WorldPositionServer, 4f, progressFinishAction, interaction.Performer);
			}
			//play sound if we started progress
			if (bar != null)
            {
            	SoundManager.PlayNetworkedAtPos("Wrench", registerObject.WorldPositionServer, 1f);
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
				var bar = UIManager.ServerStartProgress(ProgressAction.Construction, registerObject.WorldPositionServer, 4f, progressFinishAction, interaction.Performer);
				if (bar != null)
				{
					SoundManager.PlayNetworkedAtPos("screwdriver#", registerObject.WorldPositionServer, 1f);
				}

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
}