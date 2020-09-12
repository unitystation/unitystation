using UnityEngine;
using Mirror;
using Random = UnityEngine.Random;

/// <summary>
/// A grille GameObject for unsecured grilles. Secured ones are a tile instead.
/// </summary>
public class GrilleObject : NetworkBehaviour, IExaminable, ICheckedInteractable<HandApply>
{
	[Tooltip("Layer tile which this will create when screwed in place.")]
	[SerializeField]
	private LayerTile layerTile = default;

	[Tooltip("Prefab to spawn when deconstructed.")]
	[SerializeField]
	private GameObject transformToPrefab = default;

	[Tooltip("Amount to spawn of the aforementioned prefab.")]
	[SerializeField]
	private int spawnAmount = 2;

	private RegisterObject registerObject;

	private HandApply interaction;
	private string performerName;

	#region Lifecycle

	private void Start()
	{
		registerObject = GetComponent<RegisterObject>();
		GetComponent<Integrity>().OnWillDestroyServer.AddListener(OnWillDestroyServer);
	}

	private void OnWillDestroyServer(DestructionInfo arg0)
	{
		Spawn.ServerPrefab(transformToPrefab, gameObject.TileWorldPosition().To3Int(), transform.parent, count: spawnAmount);
	}

	#endregion Lifecycle

	public string Examine(Vector3 worldPos = default)
	{
		return "The anchoring screws are <i>unscrewed</i>. The rods look like they could be <b>cut</b> through.";
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		//start with the default HandApply WillInteract logic.
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		//only care about interactions targeting us
		if (interaction.TargetObject != gameObject) return false;
		// Only try to interact if the user has a screwdriver or wirecutter in their hand.
		if (!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver) &&
			!Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wirecutter)) return false;
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		this.interaction = interaction;
		performerName = interaction.Performer.ExpensiveName();

		if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver))
		{
			SecureGrille();
		}
		else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wirecutter))
		{
			CutGrille();
		}
	}

	private void SecureGrille()
	{
		// Don't secure it if there's no floor.
		if (MatrixManager.IsSpaceAt(registerObject.WorldPositionServer, true))
		{
			Chat.AddExamineMsg(interaction.Performer, "A floor must be present to secure the grille!");
			return;
		}

		if (ServerValidations.IsAnchorBlocked(interaction)) return;

		ToolUtils.ServerUseToolWithActionMessages(
				interaction, 0.5f,
				"You start securing the grille...",
				$"{performerName} starts securing the grille...",
				"You secure the grille.",
				$"{performerName} secures the grille.",
				ScrewInPlace
		);
	}

	private void CutGrille()
	{
		ToolUtils.ServerUseToolWithActionMessages(interaction, 1f,
				"You start to disassemble the grille...",
				$"{performerName} starts to disassemble the grille...",
				"You disassemble the grille.",
				$"{performerName} disassembles the grille.",
				Disassemble
		);
	}

	[Server]
	private void Disassemble()
	{
		Spawn.ServerPrefab(transformToPrefab, registerObject.WorldPositionServer, count: spawnAmount);
		ToolUtils.ServerPlayToolSound(interaction);
		Despawn.ServerSingle(gameObject);
	}

	[Server]
	private void ScrewInPlace()
	{
		var interactableTiles = InteractableTiles.GetAt(interaction.TargetObject.TileWorldPosition(), true);
		Vector3Int cellPos = interactableTiles.WorldToCell(interaction.TargetObject.TileWorldPosition());
		interactableTiles.TileChangeManager.UpdateTile(cellPos, layerTile);
		interactableTiles.TileChangeManager.SubsystemManager.UpdateAt(cellPos);
		Despawn.ServerSingle(gameObject);
	}
}
