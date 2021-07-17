using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Objects.Construction;
using Machines;
using ScriptableObjects;

namespace Objects.Machines
{
	/// <summary>
	/// Main Component for Machine deconstruction
	/// </summary>
	public class Machine : MonoBehaviour, ICheckedInteractable<HandApply>, IServerSpawn
	{
		/// <summary>
		/// Machine parts used to build this machine
		/// </summary>
		public MachineParts MachineParts;

		//Not needed on all machine prefabs
		private IDictionary<ItemTrait, int> basicPartsUsed = new Dictionary<ItemTrait, int>();
		private IDictionary<GameObject, int> partsInFrame = new Dictionary<GameObject, int>();

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

		/// <summary>
		/// Does this machine need to be able to move before allowing deconstruction?
		/// </summary>
		[Tooltip("Does this machine need to be able to move before allowing deconstruction?")]
		[SerializeField]
		private bool mustBeUnanchored;

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

			if (mustBeUnanchored && gameObject.GetComponent<PushPull>()?.IsPushable == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"The {gameObject.ExpensiveName()} needs to be unanchored first.");
				return;
			}

			//unscrew
			ToolUtils.ServerUseToolWithActionMessages(interaction, secondsToScrewdrive,
				$"You start to deconstruct the {gameObject.ExpensiveName()}...",
				$"{interaction.Performer.ExpensiveName()} starts to deconstruct the {gameObject.ExpensiveName()}...",
				$"You deconstruct the {gameObject.ExpensiveName()}.",
				$"{interaction.Performer.ExpensiveName()} deconstructs the {gameObject.ExpensiveName()}.",
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

			SpawnResult frameSpawn = Spawn.ServerPrefab(CommonPrefabs.Instance.MachineFrame, SpawnDestination.At(gameObject));
			if (!frameSpawn.Successful)
			{
				Logger.LogError($"Failed to spawn frame! Is {this} missing references in the inspector?", Category.Construction);
				return;
			}

			GameObject frame = frameSpawn.GameObject;
			frame.GetComponent<MachineFrame>().ServerInitFromComputer(this);

			_ = Despawn.ServerSingle(gameObject);

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

			if (partsInFrame == null)
			{
				Logger.LogError($"PartsInFrame was null on {gameObject.ExpensiveName()}");
				return;
			}

			var toRefresh = GetComponents<IRefreshParts>();

			foreach (var refresh in toRefresh)
			{
				refresh.RefreshParts(partsInFrame);
			}
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			//Only do so on mapping
			if(partsInFrame != null && partsInFrame.Count > 0) return;

			if (basicPartsUsed == null)
			{
				Logger.LogError($"BasicPartsUsed was null on {gameObject.ExpensiveName()}");
				return;
			}
			//Means we are mapped so use machine parts ist
			else if (basicPartsUsed.Count == 0)
			{
				if (MachineParts.OrNull()?.machineParts == null)
				{
					Logger.LogError($"MachineParts was null on {gameObject.ExpensiveName()}");
					return;
				}

				foreach (var part in MachineParts.machineParts)
				{
					basicPartsUsed.Add(part.itemTrait, part.amountOfThisPart);
				}
			}

			var toRefresh = GetComponents<IInitialParts>();

			foreach (var refresh in toRefresh)
			{
				refresh.InitialParts(basicPartsUsed);
			}
		}
	}

	public interface IRefreshParts
	{
		void RefreshParts(IDictionary<GameObject, int> partsInFrame);
	}

	public interface IInitialParts
	{
		//This will be called before RefreshParts when building a new machine
		void InitialParts(IDictionary<ItemTrait, int> basicPartsUsed);
	}
}
