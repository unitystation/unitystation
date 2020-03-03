using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Random = UnityEngine.Random;

/// <summary>
/// The main girder component
/// </summary>
public class ReinforcedWindowObject : NetworkBehaviour, ICheckedInteractable<HandApply>
{

	private RegisterObject registerObject;
	private ObjectBehaviour objectBehaviour;

	[Header("Tile creation variables")]
	[Tooltip("Layer tile which this will create.")]
	public LayerTile layerTile;

	[Header("Deconstruction variables")]
	[Tooltip("Items to drop when deconstructed.")]
	public GameObject matsOnDeconstruct;
	[Tooltip("Quantity of mats when deconstructed.")]
	public int countOfMatsOnDissasemle;
	[Tooltip("Sound on deconstruction.")]
	public string soundOnDeconstruct;

	[Header("Destroyed variables.")]
	[Tooltip("Drops this when broken with force.")]
	public GameObject matsOnDestroy;

	[Tooltip("Drops this count when destroyed.")]
	public int minCountOfMatsOnDestroy;

	[Tooltip("Drops this count when destroyed.")]
	public int maxCountOfMatsOnDestroy;

	[Tooltip("Sound when destroyed.")]
	public string soundOnDestroy;

	

	private void Start()
	{
		registerObject = GetComponent<RegisterObject>();
		GetComponent<Integrity>().OnWillDestroyServer.AddListener(OnWillDestroyServer);
		objectBehaviour = GetComponent<ObjectBehaviour>();
	}


	private void OnWillDestroyServer(DestructionInfo arg0)
	{
		Spawn.ServerPrefab(matsOnDestroy, gameObject.TileWorldPosition().To3Int(), transform.parent, count: Random.Range(minCountOfMatsOnDestroy, maxCountOfMatsOnDestroy + 1),
			scatterRadius: Random.Range(0, 3), cancelIfImpassable: true);

		SoundManager.PlayNetworkedAtPos(soundOnDestroy, gameObject.TileWorldPosition().To3Int(), 1f);
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		//start with the default HandApply WillInteract logic.
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		//only care about interactions targeting us
		if (interaction.TargetObject != gameObject) return false;

		if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench) &&
			!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver))  return false; 

		return true;
	}
	public void ServerPerformInteraction(HandApply interaction)
	{
		if (interaction.TargetObject != gameObject) return;

		
		if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver))
		{
			if (objectBehaviour.IsPushable)
			{
				//secure it if there's floor
				if (MatrixManager.IsSpaceAt(registerObject.WorldPositionServer, true))
				{
					Chat.AddExamineMsg(interaction.Performer, "A floor must be present to secure the window!");
					return;
				}

				if (!ServerValidations.IsAnchorBlocked(interaction))
				{

					ToolUtils.ServerUseToolWithActionMessages(interaction, 4f,
						"You start securing the window...",
						$"{interaction.Performer.ExpensiveName()} starts securing the window...",
						"You secure the window.",
						$"{interaction.Performer.ExpensiveName()} secures the window.",
						() => ScrewToFloor(interaction));
					return;
				}
			}
			else
			{
				//unsecure it
				ToolUtils.ServerUseToolWithActionMessages(interaction, 4f,
					"You start unsecuring the girder...",
					$"{interaction.Performer.ExpensiveName()} starts unsecuring the window...",
					"You unsecure the window.",
					$"{interaction.Performer.ExpensiveName()} unsecures the window.",
					() => objectBehaviour.ServerSetAnchored(false, interaction.Performer));
			}

		}
		else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench))
		{
			//disassemble if it's unanchored
			if (objectBehaviour.IsPushable)
			{
				ToolUtils.ServerUseToolWithActionMessages(interaction, 4f,
					"You start to disassemble the window...",
					$"{interaction.Performer.ExpensiveName()} starts to disassemble the window...",
					"You disassemble the window.",
					$"{interaction.Performer.ExpensiveName()} disassembles the window.",
					() => Disassemble(interaction));
				return;
			}
			else
			{
				Chat.AddExamineMsg(interaction.Performer, "You must unsecure it first.");
			}
		}
		
	}

	[Server]
	private void ScrewToFloor(HandApply interaction)
	{
		var interactableTiles = InteractableTiles.GetAt(interaction.TargetObject.TileWorldPosition(), true);
		Vector3Int cellPos = interactableTiles.WorldToCell(interaction.TargetObject.TileWorldPosition());
		interactableTiles.TileChangeManager.UpdateTile(cellPos, layerTile);
		interactableTiles.TileChangeManager.SubsystemManager.UpdateAt(cellPos);
		Despawn.ServerSingle(gameObject);
	}
	[Server]
	private void Disassemble(HandApply interaction)
	{
		Spawn.ServerPrefab(matsOnDeconstruct, registerObject.WorldPositionServer, count: countOfMatsOnDissasemle);
		SoundManager.PlayNetworkedAtPos(soundOnDeconstruct, gameObject.TileWorldPosition().To3Int(), 1f);
		Despawn.ServerSingle(gameObject);
	}

}
