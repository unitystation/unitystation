using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Machines
{
	/// <summary>
	/// Main Component for Machine deconstruction
	/// </summary>
	public class Machine : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		/// <summary>
		/// Machine parts used to build this machine
		/// </summary>
		public MachineParts MachineParts;

		//Not needed on all machine prefabs
		private IDictionary<ItemTrait, int> basicPartsUsed = new Dictionary<ItemTrait, int>();
		private IDictionary<GameObject, int> partsInFrame = new Dictionary<GameObject, int>();

		[Tooltip("Frame prefab this computer should deconstruct into.")]
		[SerializeField]
		private GameObject framePrefab = null;

		[Tooltip("Prefab of the circuit board that lives inside this computer.")]
		[SerializeField]
		private GameObject machineBoardPrefab = null;

		public IDictionary<ItemTrait, int> BasicPartsUsed => basicPartsUsed;
		public IDictionary<GameObject, int> PartsInFrame => partsInFrame;

		/// <summary>
		/// Prefab of the circuit board that lives inside this computer.
		/// </summary>
		public GameObject MachineBoardPrefab => machineBoardPrefab;

		/// <summary>
		/// Can this machine not be deconstructed?
		/// </summary>
		public bool canNotBeDeconstructed;

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
			if (MachineParts == null) return;

			if (canNotBeDeconstructed)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "This machine is too well built to be deconstructed.");
				return;
			}

			//unscrew
			ToolUtils.ServerUseToolWithActionMessages(interaction, secondsToScrewdrive,
				"You start to deconstruct the machine...",
				$"{interaction.Performer.ExpensiveName()} starts to deconstruct the machine...",
				"You deconstruct the machine.",
				$"{interaction.Performer.ExpensiveName()} deconstructs the machine.",
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

			SpawnResult frameSpawn = Spawn.ServerPrefab(framePrefab, SpawnDestination.At(gameObject));
			if (!frameSpawn.Successful)
			{
				Logger.LogError($"Failed to spawn frame! Is {this} missing reference to {nameof(framePrefab)} in the inspector?");
				return;
			}

			GameObject frame = frameSpawn.GameObject;
			frame.GetComponent<MachineFrame>().ServerInitFromComputer(this);

			Despawn.ServerSingle(gameObject);

			integrity.OnWillDestroyServer.RemoveListener(WhenDestroyed);
		}

		public void SetMachineParts(MachineParts machineParts)
		{
			MachineParts = machineParts;
		}

		public void SetBasicPartsUsed(IDictionary<ItemTrait, int> basicPartsUsed)
		{
			this.basicPartsUsed = basicPartsUsed;
		}

		public void SetPartsInFrame(IDictionary<GameObject, int> partsInFrame)
		{
			this.partsInFrame = partsInFrame;
		}
	}
}
