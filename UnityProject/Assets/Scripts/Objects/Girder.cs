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
	public GameObject metalPrefab;

	private RegisterObject registerObject;

	private void Start(){
		tileChangeManager = GetComponentInParent<TileChangeManager>();
		registerObject = GetComponent<RegisterObject>();
		GetComponent<Integrity>().OnWillDestroyServer.AddListener(OnWillDestroyServer);
	}

	private void OnWillDestroyServer(DestructionInfo arg0)
	{
		Spawn.ServerPrefab("Metal", gameObject.TileWorldPosition().To3Int(), transform.parent, count: 1,
			scatterRadius: Spawn.DefaultScatterRadius);
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		//start with the default HandApply WillInteract logic.
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		//only care about interactions targeting us
		if (interaction.TargetObject != gameObject) return false;
		//only try to interact if the user has a wrench or metal in their hand
		if (!Validations.HasComponent<Metal>(interaction.HandObject) &&
		    !Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench)) return false;
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (interaction.TargetObject != gameObject) return;

		if (Validations.HasComponent<Metal>(interaction.HandObject)){
			var progressFinishAction = new ProgressCompleteAction(() =>
						ConstructWall(interaction));
			UIManager.ServerStartProgress(ProgressAction.Construction, registerObject.WorldPositionServer, 5f, progressFinishAction, interaction.Performer);
		}
		else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench))
		{

			var progressFinishAction = new ProgressCompleteAction(Disassemble);
			var bar = UIManager.ServerStartProgress(ProgressAction.Construction, registerObject.WorldPositionServer, 5f, progressFinishAction, interaction.Performer);
			if (bar != null)
			{
				SoundManager.PlayNetworkedAtPos("Wrench", transform.localPosition, 1f);
			}
		}
	}

	[Server]
	private void Disassemble()
	{
		Spawn.ServerPrefab(metalPrefab, registerObject.WorldPositionServer);
		GetComponent<CustomNetTransform>().DisappearFromWorldServer();
	}

	[Server]
	private void ConstructWall(HandApply interaction){
		var handObj = interaction.HandObject;
		tileChangeManager.UpdateTile(Vector3Int.RoundToInt(transform.localPosition), TileType.Wall, "Wall");
		Inventory.ServerDespawn(interaction.HandSlot);
	}

}