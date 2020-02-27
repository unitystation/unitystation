using UnityEngine;
using Mirror;
using Random = UnityEngine.Random;

public class GrilleObject : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	private TileChangeManager tileChangeManager;

	private RegisterObject registerObject;
	private ObjectBehaviour objectBehaviour;

	[Tooltip("Layer tile which this will create when screwed in place.")]
	public LayerTile layerTile;

	private void Start()
	{
		tileChangeManager = GetComponentInParent<TileChangeManager>();
		registerObject = GetComponent<RegisterObject>();
		GetComponent<Integrity>().OnWillDestroyServer.AddListener(OnWillDestroyServer);
		objectBehaviour = GetComponent<ObjectBehaviour>();
	}

	private void OnWillDestroyServer(DestructionInfo arg0)
	{
		SoundManager.PlayNetworkedAtPos("GrillHit", gameObject.TileWorldPosition().To3Int(), Random.Range(0.9f, 1.1f));
		Spawn.ServerPrefab("Rods", gameObject.TileWorldPosition().To3Int(), transform.parent, count: 1,
			scatterRadius: Random.Range(0.9f, 1.8f), cancelIfImpassable: true);
		
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		//start with the default HandApply WillInteract logic.
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		//only care about interactions targeting us
		if (interaction.TargetObject != gameObject) return false;
		//only try to interact if the user has a wrench, screwdriver, metal, or plasteel in their hand
		if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver) &&
			!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wirecutter)) return false;
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (interaction.TargetObject != gameObject) return;

		else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver))
		{
			if (objectBehaviour.IsPushable)
			{
				//secure it if there's floor
				if (MatrixManager.IsSpaceAt(registerObject.WorldPositionServer, true))
				{
					Chat.AddExamineMsg(interaction.Performer, "A floor must be present to secure the grille!");
					return;
				}

				if (!ServerValidations.IsAnchorBlocked(interaction))
				{
					ToolUtils.ServerUseToolWithActionMessages(interaction, 0.5f,
						"You start securing the grille...",
						$"{interaction.Performer.ExpensiveName()} starts securing the grille...",
						"You secure the grille.",
						$"{interaction.Performer.ExpensiveName()} secures the grille.",
						() => ScrewInPlace(interaction));
					Despawn.ServerSingle(gameObject);

					return;

				}
			}
			else
			{
				//unsecure it
				ToolUtils.ServerUseToolWithActionMessages(interaction, 0.5f,
					"You start unsecuring the grille...",
					$"{interaction.Performer.ExpensiveName()} starts unsecuring the grille...",
					"You unsecure the grille.",
					$"{interaction.Performer.ExpensiveName()} unsecures the grille.",
					() => objectBehaviour.ServerSetAnchored(false, interaction.Performer));
				SoundManager.PlayNetworkedAtPos("screwdriver2", gameObject.TileWorldPosition().To3Int(), Random.Range(0.9f, 1.1f));
			}

		}
		else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wirecutter))
		{

			ToolUtils.ServerUseToolWithActionMessages(interaction, 1f,
				"You start to disassemble the grille...",
				$"{interaction.Performer.ExpensiveName()} starts to disassemble the grille...",
				"You disassemble the grille.",
				$"{interaction.Performer.ExpensiveName()} disassembles the grille.",
				() => Disassemble(interaction));
			Despawn.ServerSingle(gameObject);

			return;

		}
	}

	[Server]
	private void Disassemble(HandApply interaction)
	{
		Spawn.ServerPrefab("Rods", registerObject.WorldPositionServer, count: 2);
		SoundManager.PlayNetworkedAtPos("wirecutter", gameObject.TileWorldPosition().To3Int(), Random.Range(0.9f, 1.1f));	
	}

	[Server]
	private void ScrewInPlace(HandApply interaction)
	{
		tileChangeManager.UpdateTile(Vector3Int.RoundToInt(transform.localPosition), layerTile);
		interaction.HandObject.GetComponent<Stackable>().ServerConsume(2);
		SoundManager.PlayNetworkedAtPos("screwdriver2", gameObject.TileWorldPosition().To3Int(), Random.Range(0.9f, 1.1f));
		
	}

}