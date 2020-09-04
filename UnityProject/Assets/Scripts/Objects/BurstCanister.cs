using UnityEngine;

public class BurstCanister : MonoBehaviour, ICheckedInteractable<HandApply>, IExaminable
{
	[Tooltip("The prefab to spawn when the burst canister is salvaged.")]
	[SerializeField]
	private GameObject metalPrefab = default;

	[Tooltip("How much of the indicated prefab should spawn")]
	[SerializeField] [Range(1, 5)]
	private int spawnCount = 2;

	[Tooltip("Time required for the welder to salvage the burst canister.")]
	[SerializeField] [Range(0, 5)]
	private int timeToSalvage = 2;

	private HandApply salvageInteraction;

	#region Lifecycle

	private void Awake()
	{
		// Send the component to sleep until something bursts the canister.
		enabled = false;
	}

	#endregion Lifecycle

	#region Interaction

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		return Validations.HasUsedActiveWelder(interaction);
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		salvageInteraction = interaction;
		TrySalvageMetal();
	}

	private void TrySalvageMetal()
	{
		ToolUtils.ServerUseToolWithActionMessages(
				salvageInteraction, timeToSalvage,
				"You start cutting up the burst canister into sheets...",
				$"{salvageInteraction.Performer.ExpensiveName()} starts cutting up the burst canister...",
				"You finish cutting up the burst canister.",
				$"{salvageInteraction.Performer.ExpensiveName()} finishes cutting up the burst canister.",
				SalvageMetal
		);
	}

	private void SalvageMetal()
	{
		Spawn.ServerPrefab(metalPrefab, gameObject.RegisterTile().WorldPositionServer, count: spawnCount);
		Despawn.ServerSingle(gameObject);
	}

	public string Examine(Vector3 worldPos = default)
	{
		return "This canister has burst and is now useless. Perhaps you could salvage it?";
	}

	#endregion Interaction
}
