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
		public IDictionary<ItemTrait, int> basicPartsUsed = new Dictionary<ItemTrait, int>();
		public IDictionary<ItemTrait, int> partsUsed = new Dictionary<ItemTrait, int>();

		[Tooltip("Frame prefab this computer should deconstruct into.")]
		[SerializeField]
		private GameObject framePrefab = null;

		[Tooltip("Prefab of the circuit board that lives inside this computer.")]
		[SerializeField]
		private GameObject machineBoardPrefab = null;

		/// <summary>
		/// Prefab of the circuit board that lives inside this computer.
		/// </summary>
		public GameObject MachineBoardPrefab => machineBoardPrefab;

		[Tooltip("Time taken to screwdrive to deconstruct this.")]
		[SerializeField]
		private float secondsToScrewdrive = 2f;

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
					//drop all our contents
					ItemStorage itemStorage = null;
					// rare cases were gameObject is destroyed for some reason and then the method is called

					if (gameObject != null)
					{
						itemStorage = GetComponent<ItemStorage>();
					}

					if (itemStorage != null)
					{
						itemStorage.ServerDropAll();
					}

					var frame = Spawn.ServerPrefab(framePrefab, SpawnDestination.At(gameObject)).GameObject;
					frame.GetComponent<MachineFrame>().ServerInitFromComputer(this);
					Despawn.ServerSingle(gameObject);
				});
		}

		public void SetMachineParts(MachineParts machineParts)
		{
			MachineParts = machineParts;
		}

		public void SetBasicPartsUsed(IDictionary<ItemTrait, int> BasicPartsUsed)
		{
			basicPartsUsed = BasicPartsUsed;
		}

		public void SetPartsUsed(IDictionary<ItemTrait, int> PartsUsed)
		{
			partsUsed = PartsUsed;
		}
	}
}