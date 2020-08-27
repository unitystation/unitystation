using UnityEngine;


/// <summary>
/// Main behavior for computers. Allows them to be deconstructed to frames.
/// </summary>
public class Computer : MonoBehaviour, ICheckedInteractable<HandApply>
{

	[Tooltip("Frame prefab this computer should deconstruct into.")]
	[SerializeField]
	private GameObject framePrefab = null;

	[Tooltip("Prefab of the circuit board that lives inside this computer.")]
	[SerializeField]
	private GameObject circuitBoardPrefab = null;

	/// <summary>
	/// Prefab of the circuit board that lives inside this computer.
	/// </summary>
	public GameObject CircuitBoardPrefab => circuitBoardPrefab;

	[Tooltip("Time taken to screwdrive to deconstruct this.")]
	[SerializeField]
	private float secondsToScrewdrive = 2f;

	private Integrity integrity;

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		if (!Validations.IsTarget(gameObject, interaction)) return false;

		return Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver);
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		//unscrew
		ToolUtils.ServerUseToolWithActionMessages(interaction, secondsToScrewdrive,
			"You start to disconnect the monitor...",
			$"{interaction.Performer.ExpensiveName()} starts to disconnect the monitor...",
			"You disconnect the monitor.",
			$"{interaction.Performer.ExpensiveName()} disconnects the monitor.",
			() =>
			{
				WhenDestroyed(null);
			});
	}

	private void Awake()
	{
		if (!CustomNetworkManager.IsServer) return;

		integrity = GetComponent<Integrity>();

		integrity.OnWillDestroyServer.AddListener(WhenDestroyed);
	}

	public void WhenDestroyed(DestructionInfo info)
	{
		//drop all our contents
		ItemStorage itemStorage = null;
		// rare cases were gameObject is destroyed for some reason and then the method is called
		if (gameObject == null) return;

		itemStorage = GetComponent<ItemStorage>();

		if (itemStorage != null)
		{
			itemStorage.ServerDropAll();
		}
		var frame = Spawn.ServerPrefab(framePrefab, SpawnDestination.At(gameObject)).GameObject;
		frame.GetComponent<ComputerFrame>().ServerInitFromComputer(this);
		Despawn.ServerSingle(gameObject);

		integrity.OnWillDestroyServer.RemoveListener(WhenDestroyed);
	}
}
