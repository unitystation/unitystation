using System.Collections.Generic;
using Items;
using Logs;
using UnityEngine;
using ScriptableObjects;
using Systems.Hacking;
using Objects.Construction;
using Machines;
using Messages.Server;
using Messages.Server.SoundMessages;

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
		private IDictionary<GameObject, int> activeGameObjectpartsInFrame = new Dictionary<GameObject, int>();

		private IDictionary<PartReference, int> ObjectpartsInFrame = new Dictionary<PartReference, int>();


		[Tooltip("Prefab of the circuit board that lives inside this computer.")] [SerializeField]
		private GameObject machineBoardPrefab = null;

		public IDictionary<GameObject, int> ActiveGameObjectpartsInFrame => activeGameObjectpartsInFrame;

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
						$"You unscrews the {gameObject.ExpensiveName()}'s cable panel.",
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

			if (MachineParts == null) return;

			if (canNotBeDeconstructed)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer,
					"This machine is too well built to be deconstructed.");
				return;
			}

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

		private void Awake()
		{
			HackingProcessBase = GetComponent<HackingProcessBase>();
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
				Loggy.LogError($"Failed to spawn frame! Is {this} missing references in the inspector?",
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

		public void SetPartsInFrame(IDictionary<GameObject, int> InActiveGameObjectpartsInFrame) //Presume that it is all the it needs parts!!
		{
			this.activeGameObjectpartsInFrame = InActiveGameObjectpartsInFrame;

			if (InActiveGameObjectpartsInFrame == null)
			{
				Loggy.LogError($"PartsInFrame was null on {gameObject.ExpensiveName()}");
				return;
			}

			ObjectpartsInFrame.Clear();


			foreach (var KVP in activeGameObjectpartsInFrame)
			{
				var itemAV2 = KVP.Key.GetComponent<ItemAttributesV2>();
				ItemTrait itemTrait = null;

				for(int i = 0; i < MachineParts.machineParts.Length; i++)
				{
					// If the interaction object has an itemtrait thats in the list, set the list machinePartsList variable as the list from the machineParts data from the circuit board.
					if (itemAV2.HasTrait(MachineParts.machineParts[i].itemTrait))
					{
						itemTrait = MachineParts.machineParts[i].itemTrait;
						break;

						// IF YOU WANT AN ITEM TO HAVE TWO ITEMTTRAITS WHICH CONTRIBUTE TO THE MACHINE BUILIDNG PROCESS, THIS NEEDS TO BE REFACTORED
						// all the stuff below needs to go into its own method which gets called here, replace the break;
					}
				}

				var StockTier = KVP.Key.GetComponent<StockTier>();

				ObjectpartsInFrame.Add(new PartReference()
				{
					itemTrait = itemTrait,
					tier = StockTier.OrNull()?.Tier ?? -1
				}, KVP.Value);
			}

			var toRefresh = GetComponents<IRefreshParts>();

			foreach (var refresh in toRefresh)
			{
				refresh.RefreshParts(ObjectpartsInFrame, this);
			}
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			//Only do so on mapping
			if (activeGameObjectpartsInFrame != null && activeGameObjectpartsInFrame.Count > 0) return;

			if (activeGameObjectpartsInFrame == null)
			{
				Loggy.LogError($"BasicPartsUsed was null on {gameObject.ExpensiveName()}");
				return;
			}
			//Means we are mapped so use machine parts ist
			else if (ObjectpartsInFrame.Count == 0)
			{
				if (MachineParts.OrNull()?.machineParts == null)
				{
					if (canNotBeDeconstructed == false)
					{
						Loggy.LogError($"MachineParts was null on {gameObject.ExpensiveName()}");
					}

					return;
				}

				foreach (var part in MachineParts.machineParts)
				{
					var Intier = part.tier;

					if (Intier == -1 && MachinePartsItemTraits.Instance.IsComponent(part.itemTrait))
					{
						//IS legacy settings reeeea
						Intier = 1;
					}

					ObjectpartsInFrame.Add(new PartReference()
					{
						itemTrait = part.itemTrait,
						tier = Intier,
					}, part.amountOfThisPart);
				}
			}

			var toRefresh = GetComponents<IRefreshParts>();

			foreach (var refresh in toRefresh)
			{
				refresh.RefreshParts(ObjectpartsInFrame, this);
			}
		}

		public bool GetPanelOpen()
		{
			return panelopen;
		}


		//Used for if you have a bass performance Stat And you want to * it depending on how many advanced parts there are
		//Maxes out at 4
		public float GetPartMultiplier()
		{
			float TotalParts = 0;
			float Alladded = 0;
			foreach (var Objectpart in ObjectpartsInFrame)
			{
				if (Objectpart.Key.tier == -1) continue;

				TotalParts += Objectpart.Value;

				for (int i = 0; i < Objectpart.Value; i++)
				{
					Alladded += Objectpart.Key.tier;
				}
			}

			return Alladded / TotalParts;
		}

		//Used for if you have a bass performance Stat And you want to * it depending on how many advanced parts there are
		//Maxes out at 4
		public float GetCertainPartMultiplier(ItemTrait ItemTrait)
		{
			if (ItemTrait == null)
			{
				Loggy.LogError($" null ItemTrait Tried to be passed into GetCertainPartMultiplier for {this.name} ");
				return 1;
			}
			float TotalParts = 0;
			float Alladded = 0;
			foreach (var Objectpart in ObjectpartsInFrame)
			{
				if (Objectpart.Key.tier == -1) continue;
				if (ItemTrait != Objectpart.Key.itemTrait) continue;
				TotalParts += Objectpart.Value;

				for (int i = 0; i < Objectpart.Value; i++)
				{
					Alladded += Objectpart.Key.tier;
				}
			}

			if (TotalParts == 0)
			{
				Loggy.LogError($"Warning {ItemTrait.name} was not present on {this.name} somehow ");
				return 1;
			}
			return Alladded / TotalParts;
		}
	}


	public interface IRefreshParts
	{
		void RefreshParts(IDictionary<PartReference, int> partsInFrame, Machine Frame);
	}


	public class PartReference
	{
		public ItemTrait itemTrait;
		public GameObject itemObject;
		public int tier = -1;
	}
}