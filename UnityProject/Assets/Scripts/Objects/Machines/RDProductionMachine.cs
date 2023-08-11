using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Systems.Electricity;
using Objects.Machines;
using ScriptableObjects.Systems.Research;
using Shared.Systems.ObjectConnection;

namespace Systems.Research.Objects
{
	[RequireComponent(typeof(MaterialStorageLink))]
	public class RDProductionMachine: NetworkBehaviour, ICheckedInteractable<HandApply>, IAPCPowerable, IMultitoolSlaveable
	{
		[SyncVar(hook = nameof(SyncSprite))]
		private RDProState stateSync;

		[SerializeField]
		private SpriteHandler spriteHandler;

		[SerializeField]
		private SpriteDataSO idleSprite;

		[SerializeField]
		private SpriteDataSO productionSprite;

		[SerializeField]
		private SpriteDataSO acceptingMaterialsSprite;

		[SerializeField]
		private MachineType machineType;
		[SerializeField]
		private Department MachineDepartment;

		public PowerState PoweredState;

		private ItemTrait InsertedMaterialType;
		public MaterialStorageLink materialStorageLink;
		public MaterialStorage Storage;
		public RegisterObject registerObject;

		public ResearchServer researchServer;
		private string Machinerytype;
		private string department;

		public Dictionary<string, List<string>> Categories = null;
		[SerializeField]
		private RDProCategoryListSO CategoryList;

		[SerializeField]
		private RDProInitialProductsSO InitialDesigns;
		[HideInInspector]
		public List<string> AvailableForMachine;

		public delegate void MaterialsManipulating();
		public event MaterialsManipulating MaterialsManipulated;

		private IEnumerator currentProduction;

		static CustomNetworkManager networkManager;

		[SerializeField] private DesignProductionData designProductionData;

		public enum RDProState
		{
			Idle,
			AcceptingMaterials,
			Production,
		}

		public enum MachineType
		{
			ProtoLathe = 0,
			CircuitImprinter = 1,
			ExosuitFabricator = 2,
		}

		public enum Department
		{
			Science = 0,
			Engineering = 1,
			Cargo = 2,
			Security = 3,
			Medical = 4,
			Service = 5,
			All = 6,
		}

		#region TechwebInteraction

		//Adds a list of Designs to the list of produceables and sorts them into categories (If valid).
		public void AddDesigns(List<string> DesignList)
		{
			foreach (string ResearchedDesign in DesignList)
			{
				if (Designs.Globals.InternalIDSearch.ContainsKey(ResearchedDesign))
				{
					Design designClass = Designs.Globals.InternalIDSearch[ResearchedDesign];

					if (designClass.MachineryType.Contains(Machinerytype) && (department == "All" || designClass.CompatibleMachinery.Contains(department)))
					{
						if (!AvailableForMachine.Contains(ResearchedDesign))
						{
							AvailableForMachine.Add(ResearchedDesign);

							bool ValidCategory = false;

							foreach (string cat in designClass.Category) //Sorting all the designs into categories for the GUI to use- if it does not belong to any of the categories for this machine- but a misc category exists- add it to that instead.
							{

								if (Categories.ContainsKey(cat))
								{
									Categories[cat].Add(ResearchedDesign);
									ValidCategory = true;
								}
							}
							if (!ValidCategory && Categories["Misc"] != null)
							{
								Categories["Misc"].Add(ResearchedDesign);
							}

						}

					}
				}
				else
				{
					Debug.LogWarning("Design '" + ResearchedDesign + "' does not exist. Please check spelling matches Internal_ID");
				}
			}
		}

		//Clears Designs and Categories for whenever the Techweb hardrive is removed or disconnected and readds InitialDesigns.
		public void OnRemoveTechweb()
		{
			AvailableForMachine.Clear();

			foreach(KeyValuePair<string, List<string>> kvp in Categories)
			{
				kvp.Value.Clear();
			}

			AddDesigns(InitialDesigns.InitialDesigns);
		}

		#endregion

		#region Lifecycle

		public void Awake()
		{
			networkManager = CustomNetworkManager.Instance;
			registerObject = GetComponent<RegisterObject>();
			materialStorageLink = GetComponent<MaterialStorageLink>();

			Categories = new Dictionary<string, List<string>>();

			Machinerytype = machineType.ToString();
			department = MachineDepartment.ToString();

			foreach (string Category in CategoryList.Categories)
			{
				Categories.Add(Category, new List<string>());
			}

			OnRemoveTechweb();

			if(researchServer != null)
			{
				researchServer.Techweb.TechWebDesignUpdateEvent += TechWebUpdate;
			}
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			SyncSprite(RDProState.Idle, RDProState.Idle);
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			if (currentProduction != null)
			{
				StopCoroutine(currentProduction);
				currentProduction = null;
			}
			if (materialStorageLink != null)
			{
				materialStorageLink.Despawn();
			}
		}

		#endregion

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (interaction.HandSlot.IsEmpty) return false;
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			InsertedMaterialType = materialStorageLink.usedStorage.FindMaterial(interaction.HandObject);
			if (InsertedMaterialType != null)
			{
				return true;
			}
			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			// Can't insert materials while RDPro is in production.
			if (stateSync != RDProState.Production)
			{
				int materialSheetAmount = interaction.HandSlot.Item.GetComponent<Stackable>().Amount;
				if (materialStorageLink.TryAddSheet(InsertedMaterialType, materialSheetAmount))
				{
					interaction.HandSlot.Item.GetComponent<Stackable>().ServerConsume(materialSheetAmount);
					if (stateSync == RDProState.Idle)
					{
						StartCoroutine(AnimateAcceptingMaterials());
					}
					UpdateGUI();
				}
				else Chat.AddActionMsgToChat(interaction.Performer, "Protolathe is full",
					"Protolathe is full");
			}
			else Chat.AddActionMsgToChat(interaction.Performer, "Cannot accept materials while fabricating",
				"Cannot accept materials while fabricating");
		}

		[Server]
		public bool CanProcessProduct(string DesignID)
		{
			if (AvailableForMachine.Contains(DesignID))
			{
				Design Designclass = Designs.Globals.InternalIDSearch[DesignID];

				SerializableDictionary<MaterialSheet, int> consumeList = new SerializableDictionary<MaterialSheet, int>();

				foreach (KeyValuePair<string, int> entry in Designclass.Materials)
				{
					consumeList.Add(designProductionData.MaterialSheets[entry.Key], entry.Value);
				}

				if (materialStorageLink.usedStorage.TryConsumeList(consumeList))
				{
					if (APCPoweredDevice.IsOn(PoweredState))
					{
						currentProduction = ProcessProduction(DesignID, 0.6f);
						StartCoroutine(currentProduction);
						return true;
					}
				}
			}
			Debug.Log("Returned False");
			return false;
		}

		private IEnumerator ProcessProduction(string DesignID, float productionTime)
		{
			Design Designclass = Designs.Globals.InternalIDSearch[DesignID];

			GameObject productObject = networkManager.ForeverIDLookupSpawnablePrefabs[Designclass.ItemID];

			stateSync = RDProState.Production;
			yield return WaitFor.Seconds(productionTime);

			if (productObject != null)
			{
				Spawn.ServerPrefab(productObject, registerObject.WorldPositionServer, transform.parent, count: 1);
			}
			else
			{
				Debug.LogWarning("No gameobject found with ItemID: " + Designclass.ItemID);
			}
			stateSync = RDProState.Idle;
		}

		public void DispenseMaterialSheet(int amountOfSheets, ItemTrait materialType)
		{
			materialStorageLink.usedStorage.DispenseSheet(amountOfSheets, materialType, gameObject.AssumedWorldPosServer());
			UpdateGUI();
		}

		public void UpdateGUI()
		{
			// Delegate calls method in all subscribers when material is changed
			MaterialsManipulated?.Invoke();
		}

		private IEnumerator AnimateAcceptingMaterials()
		{
			stateSync = RDProState.AcceptingMaterials;

			yield return WaitFor.Seconds(0.9f);
			if (stateSync == RDProState.Production)
			{
				// Do nothing if production was started during the material insertion animation
			}
			else
			{
				stateSync = RDProState.Idle;
			}
		}

		public void SyncSprite(RDProState stateOld, RDProState stateNew)
		{
			stateSync = stateNew;
			if (stateNew == RDProState.Idle)
			{
				spriteHandler.SetSpriteSO(idleSprite);
			}
			else if (stateNew == RDProState.Production)
			{
				spriteHandler.SetSpriteSO(productionSprite);
			}
			else if (stateNew == RDProState.AcceptingMaterials)
			{
				spriteHandler.SetSpriteSO(acceptingMaterialsSprite);
			}
		}

		#region Multitool Interaction

		MultitoolConnectionType IMultitoolLinkable.ConType => MultitoolConnectionType.ResearchServer;
		IMultitoolMasterable IMultitoolSlaveable.Master => researchServer;
		bool IMultitoolSlaveable.RequireLink => false;

		bool IMultitoolSlaveable.TrySetMaster(GameObject performer, IMultitoolMasterable master)
		{
			SetMaster(master);
			return true;
		}

		void IMultitoolSlaveable.SetMasterEditor(IMultitoolMasterable master)
		{
			SetMaster(master);
		}

		private void SetMaster(IMultitoolMasterable master)
		{
			if (master is ResearchServer server && server != researchServer)
			{
				SubscribeToServerEvent(server);
			}
			else if (researchServer != null)
			{
				UnSubscribeFromServerEvent();
			}
		}

		public void SubscribeToServerEvent(ResearchServer server)
		{
			UnSubscribeFromServerEvent();

			server.Techweb.TechWebDesignUpdateEvent += TechWebUpdate;
			AddDesigns(server.Techweb.UpdateAvailableDesigns());
			researchServer = server;

		}

		public void UnSubscribeFromServerEvent()
		{
			OnRemoveTechweb();

			if (researchServer == null) return;
			researchServer.Techweb.TechWebDesignUpdateEvent -= TechWebUpdate;
			researchServer = null;
		}

		public void TechWebUpdate(int UpdateType, List<string> DesignList)
		{
			switch(UpdateType)
			{
				case 1: //A new node is researched or drive is added
					AddDesigns(DesignList);
					break;
				case 0: //Techweb Drive is removed or server is destroyed
					OnRemoveTechweb();
					break;
			}
		}
		#endregion

		#region IAPCPowerable

		public void PowerNetworkUpdate(float voltage) { }

		public void StateUpdate(PowerState state)
		{
			PoweredState = state;
		}

		#endregion
	}
}
