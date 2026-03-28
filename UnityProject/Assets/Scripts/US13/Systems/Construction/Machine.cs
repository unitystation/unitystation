using System.Collections.Generic;
using System.Linq;
using Logs;
using SecureStuff;
using UnityEngine;
using US13.Core.Addressables;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.Health.Objects;
using US13.Items;
using US13.Items.Engineering;
using US13.Items.Tool;
using US13.Items.Traits;
using US13.Managers;
using US13.Managers.NetworkManagement;
using US13.Messages.Server;
using US13.Messages.Server.SoundMessages;
using US13.ScriptableObjects;
using US13.Systems.Construction.Parts;
using US13.Systems.Hacking.HackingProcesses;
using US13.Systems.Inventory;
using US13.UI.Core.Net;
using Util;
using UniversalObjectPhysics = US13.Core.Physics.UniversalObjectPhysics;

namespace US13.Systems.Construction
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

		private List<PartReference> ObjectpartsInFrame = new List<PartReference>();

		public List<PartReference> getObjectpartsInFrame => ObjectpartsInFrame;

		[Tooltip("Prefab of the circuit board that lives inside this computer.")] [SerializeField]
		private GameObject machineBoardPrefab = null;


		/// <summary>
		/// Prefab of the circuit board that lives inside this computer.
		/// </summary>
		public GameObject MachineBoardPrefab => machineBoardPrefab;

		/// <summary>
		/// Can this machine not be deconstructed?
		/// </summary>
		public bool canNotBeDeconstructed;

		///<summary>
		/// Is this machine resistant to EMPs?
		///</summary>
		public bool isEMPResistant = false;

		/// <summary>
		/// Does this machine need to be able to move before allowing deconstruction?
		/// </summary>
		[Tooltip("Does this machine need to be able to move before allowing deconstruction?")] [SerializeField]
		private bool mustBeUnanchored;

		[Tooltip("Time taken to screwdrive to deconstruct this.")] [SerializeField]
		private float secondsToScrewdrive = 2f;

		private Integrity integrity;

		private bool panelopen = false;

		private HackingProcessBase HackingProcessBase;

		private float? CachedMultiplier = null;

		public ItemStorage PartsStorage;

		[PlayModeOnly] public bool MapSpawned = false;

		private IRefreshParts[] IRefreshParts;

		public Battery[] Batterys;

		public float CurrentBatteryCapacity
		{
			get
			{
				float capacity = 0f;
				int count = Batterys.Length;
				for (int i = 0; i < count; i++)
					capacity += Batterys[i].Watts;

				return capacity;
			}
			set
			{
				int count = Batterys.Length;
				if (count == 0)
					return;

				if (value <= 0f)
				{
					for (int i = 0; i < count; i++)
						Batterys[i].Watts = 0;
					return;
				}

				// Compute max capacity
				int maxWatts = 0;
				for (int i = 0; i < count; i++)
					maxWatts += Batterys[i].MaxWatts;

				if (maxWatts <= 0)
					return;

				// Compute proportion of power to apply
				float percentage = value / maxWatts;
				if (percentage > 1f)
					percentage = 1f;

				int scaled;
				for (int i = 0; i < count; i++)
				{
					scaled = Mathf.RoundToInt(Batterys[i].MaxWatts * percentage);
					Batterys[i].Watts = scaled;
				}
			}
		}

		private void Awake()
		{
			if (PartsStorage == null)
			{
				Loggy.Error($"bwawaaa Missing PartsStorage for {this.gameObject.name}");
			}

			PartsStorage.ServerInventoryItemSlotSet += PartFrameTransfer;
			HackingProcessBase = GetComponent<HackingProcessBase>();
			if (CustomNetworkManager.IsServer == false) return;

			integrity = GetComponent<Integrity>();
			integrity.OnWillDestroyServer.AddListener(WhenDestroyed);

			IRefreshParts = GetComponents<IRefreshParts>();
		}

		public void Start()
		{
			if (CustomNetworkManager.IsServer == false) return;
			//So,
			//ObjectpartsInFrame.Count == 0 Means it's not been populated from construction
			//Means we are mapped so use machine parts ist
			if (ObjectpartsInFrame.Count == 0)
			{
				MapSpawned = true;
				if (MachineParts.OrNull()?.machineParts == null)
				{
					if (canNotBeDeconstructed == false)
					{
						Loggy.Error($"MachineParts was null on {gameObject.ExpensiveName()}");
					}

					return;
				}

				var machineBoard = Spawn.ServerPrefab(machineBoardPrefab, gameObject.AssumedWorldPosServer(),
					gameObject.transform.parent).GameObject;
				PartsStorage.ServerTryAdd(machineBoard);

				foreach (var part in MachineParts.machineParts)
				{

					for (int i = 0; i < part.amountOfThisPart; i++)
					{
						var partObjs = Spawn.ServerPrefab(part.basicItem, gameObject.AssumedWorldPosServer(),
							gameObject.transform.parent).GameObjects;

						foreach (var Object in partObjs)
						{
							PartsStorage.ServerTryAdd(Object);
						}
					}

				}
			}
			else
			{
				MapSpawned = false;
			}


			var data = PartsStorage.GetItemSlots();

			var toRefresh = GetComponents<IRefreshParts>();
			UpdateBatteries();
			foreach (var refresh in toRefresh)
			{
				refresh.RefreshParts(ObjectpartsInFrame, this);
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (!Validations.IsTarget(gameObject, interaction)) return false;

			if (HackingProcessBase != null)
			{
				return Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver) ||
				       Validations.HasItemTrait(interaction,
					       CommonTraits.Instance.Crowbar) || //Should probably network if it is open or not
				       Validations.HasItemTrait(interaction, CommonTraits.Instance.Cable) ||
				       Validations.HasItemTrait(interaction, CommonTraits.Instance.Wirecutter);
			}
			else
			{
				return Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver) ||
				       Validations.HasItemTrait(interaction, CommonTraits.Instance.Crowbar);
			}
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver))
			{
				AudioSourceParameters audioSourceParameters =
					new AudioSourceParameters(pitch: UnityEngine.Random.Range(0.8f, 1.2f));
				SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.screwdriver,
					interaction.Performer.AssumedWorldPosServer(), audioSourceParameters, sourceObj: gameObject);
				//Unscrew panel
				panelopen = !panelopen;
				if (panelopen)
				{
					Chat.AddActionMsgToChat(interaction.Performer,
						$"You unscrew the {gameObject.ExpensiveName()}'s cable panel.",
						$"{interaction.Performer.ExpensiveName()} unscrews {gameObject.ExpensiveName()}'s cable panel.");
					return;
				}
				else
				{
					Chat.AddActionMsgToChat(interaction.Performer,
						$"You screw in the {gameObject.ExpensiveName()}'s cable panel.",
						$"{interaction.Performer.ExpensiveName()} screws in {gameObject.ExpensiveName()}'s cable panel.");
					return;
				}
			}

			if (HackingProcessBase != null)
			{
				if (panelopen && (Validations.HasItemTrait(interaction, CommonTraits.Instance.Cable) ||
				                  Validations.HasItemTrait(interaction, CommonTraits.Instance.Wirecutter)))
				{
					TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType.HackingPanel, TabAction.Open);
				}
			}

			if (canNotBeDeconstructed)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer,
					"This machine is too well built to be deconstructed.");
				return;
			}


			if (MachineParts == null) return;

			if (mustBeUnanchored && gameObject.GetComponent<UniversalObjectPhysics>().OrNull()?.IsNotPushable == true)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer,
					$"The {gameObject.ExpensiveName()} needs to be unanchored first.");
				return;
			}

			if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Crowbar) && panelopen)
			{
				//unsecure
				ToolUtils.ServerUseToolWithActionMessages(interaction, secondsToScrewdrive,
					$"You start to deconstruct the {gameObject.ExpensiveName()}...",
					$"{interaction.Performer.ExpensiveName()} starts to deconstruct the {gameObject.ExpensiveName()}...",
					$"You deconstruct the {gameObject.ExpensiveName()}.",
					$"{interaction.Performer.ExpensiveName()} deconstructs the {gameObject.ExpensiveName()}.",
					() => { WhenDestroyed(null); });
			}
		}

		public int NumberOfPartsForTrait(ItemTrait itemTrait)
		{
			return ObjectpartsInFrame.Where(x => x.itemTrait == itemTrait).Sum(x => x.itemObject.NumberOf());
		}

		public int RatingOfPartsForTrait(ItemTrait itemTrait)
		{
			return ObjectpartsInFrame.Where(x => x.itemTrait == itemTrait).Sum(x => x.itemObject.NumberOf() * x.tier);
		}

		public void WhenDestroyed(DestructionInfo info)
		{
			// rare cases were gameObject is destroyed for some reason and then the method is called
			if (gameObject == null) return;


			SpawnResult frameSpawn =
				Spawn.ServerPrefab(CommonPrefabs.Instance.MachineFrame, SpawnDestination.At(gameObject));
			if (!frameSpawn.Successful)
			{
				Loggy.Error($"Failed to spawn frame! Is {this} missing references in the inspector?",
					Category.Construction);
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

		public void SetPartsInFrame(ItemStorage InActiveGameObjectpartsInFrame) //Presume that it is all the it needs parts!!
		{
			CachedMultiplier = null;

			ObjectpartsInFrame.Clear();

			PartsStorage.ServerTryTransferFrom(InActiveGameObjectpartsInFrame);
		}

		public bool GetPanelOpen()
		{
			return panelopen;
		}


		//Used for if you have a bass performance Stat And you want to * it depending on how many advanced parts there are
		//Maxes out at 4
		public float GetPartMultiplier()
		{
			if (CachedMultiplier != null)
			{
				return CachedMultiplier.Value;
			}

			float TotalParts = 0;
			float Alladded = 0;
			foreach (var Objectpart in ObjectpartsInFrame)
			{
				var Number = Objectpart.itemObject.NumberOf();

				TotalParts += Number;

				Alladded += Objectpart.tier * Number;
			}

			CachedMultiplier = Alladded / TotalParts;
			return CachedMultiplier.Value;
		}

		//Used for if you have a bass performance Stat And you want to * it depending on how many advanced parts there are
		//Maxes out at 4
		public float GetCertainPartMultiplier(ItemTrait ItemTrait)
		{
			if (ItemTrait == null)
			{
				Loggy.Error($" null ItemTrait Tried to be passed into GetCertainPartMultiplier for {this.name} ");
				return 1;
			}

			float TotalParts = 0;
			float Alladded = 0;
			foreach (var Objectpart in ObjectpartsInFrame)
			{
				if (ItemTrait != Objectpart.itemTrait) continue;

				var Number = Objectpart.itemObject.NumberOf();
				TotalParts += Number;
				Alladded += Objectpart.tier * Number;
			}

			if (TotalParts == 0)
			{
				Loggy.Error($"Warning {ItemTrait.name} was not present on {this.name} somehow ");
				return 1;
			}

			return Alladded / TotalParts;
		}


		public bool? BatteryChangeChargedByDelta(int Delta)
		{

			if (Delta == 0) return null;
			foreach (var batty in Batterys)
			{
				if (Delta > 0)
				{
					var SpareCapacity = batty.MaxWatts - batty.Watts;
					if (Delta > SpareCapacity)
					{
						batty.Watts = batty.MaxWatts;
						Delta -= SpareCapacity;
					}
					else
					{
						batty.Watts += Delta;
						Delta = 0;
						break;
					}
				}
				else
				{
					var SpareCapacity = batty.Watts;
					if (Mathf.Abs(Delta) > SpareCapacity)
					{
						batty.Watts = 0;
						Delta += SpareCapacity;
					}
					else
					{
						batty.Watts += Delta;
						Delta = 0;
						break;
					}
				}

			}

			if (Mathf.Abs(Delta) > 0)
			{
				return false;
			}
			else
			{
				return true;
			}
		}


		public void PartFrameTransfer(Pickupable prevPart, Pickupable newPart)
		{
			if (MachineParts == null) return;

			if (newPart)
			{
				var attributes = newPart.GetComponent<ItemAttributesV2>();
				// TODO: Refactor to support items with multiple item traits contributing to the machine building process.
				var machinePartList = MachineParts.machineParts
					.FirstOrDefault(p => attributes.HasTrait(p.itemTrait));

				if (machinePartList != null)
				{
					var stockTier = newPart.GetComponent<StockTier>();
					ObjectpartsInFrame.Add(new PartReference()
					{
						itemObject = newPart.gameObject,
						Slot = newPart.ItemSlot,
						itemTrait = machinePartList.itemTrait,
						tier = stockTier != null ? stockTier.Tier : 1
					});
				}
			}
			else if (prevPart)
			{
				ObjectpartsInFrame.RemoveAll(x => x.itemObject == prevPart.gameObject);
			}

			UpdateBatteries();
			foreach (var refresh in IRefreshParts)
			{
				refresh.RefreshParts(ObjectpartsInFrame, this);
			}
		}

		public void UpdateBatteries()
		{
			Batterys = getObjectpartsInFrame.Where(x => x.itemTrait == CommonTraits.Instance.PowerCell)
				.Select(x => x.itemObject.GetCachedComponent<Battery>()).ToArray();
		}
	}


	public interface IRefreshParts
	{
		void RefreshParts(List<PartReference> partsInFrame, Machine Frame);
	}


	public class PartReference
	{
		public ItemTrait itemTrait;
		public ItemSlot Slot;
		public GameObject itemObject;
		public int tier = -1;
	}
}