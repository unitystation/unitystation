using System;
using System.Collections;
using AddressableReferences;
using Machines;
using Messages.Server;
using Mirror;
using Systems.Electricity;
using Systems.Interaction;
using UnityEngine;
using System.Collections.Generic;
using Core.Physics;
using ScriptableObjects.Systems.Research;
using Systems.Research;
using YamlDotNet.Serialization.NodeTypeResolvers;

namespace Objects.Machines
{
	[RequireComponent(typeof(MaterialStorageLink))]
	public class Flatpacker : MonoBehaviour, IAPCPowerable, ICheckedInteractable<HandApply>, IRefreshParts, IServerDespawn
	{
		[SerializeField] private SpriteHandler primarySpriteHandler;
		[SerializeField] private SpriteHandler overlaySpriteHandler;

		[SerializeField] private APCPoweredDevice apcPoweredDevice;
		[SerializeField] private ItemStorage loadedItemStorage = null;
		[SerializeField] private UniversalObjectPhysics objectPhysics = null;
		[SerializeField] private MaterialStorageLink materialStorageLink = null;

		[SerializeField] private DesignProductionData designProductionData = null;

		private ItemSlot _loadedMachineBoardSlot = null;

		[SerializeField] private AddressableAudioSource beginSound;
		[SerializeField] private AddressableAudioSource processingSound;
		[SerializeField] private AddressableAudioSource finishSound;

		[SerializeField] private ItemTrait machineBoardTrait = null;
		[SerializeField] private GameObject flatPackPrefab = null;
		[SerializeField] private GameObject metalSheetPrefab = null;
		[SerializeField] private GameObject cableCoilPrefab = null;

		public delegate void MaterialsManipulating();
		public static event MaterialsManipulating MaterialsManipulated;

		public delegate void MachineLoadedEvent(string machineName, SerializableDictionary<MaterialSheet, int> neededMaterials,
			Dictionary<ItemTrait, int> currentMaterials);
		public event MachineLoadedEvent OnMachineChange;

		private ItemTrait _insertedMaterialType;

		private MachineCircuitBoard _loadedMachineBoard = null;
		private readonly List<string> _partsToSpawn = new List<string>();
		private readonly SerializableDictionary<MaterialSheet, int> _neededMaterials = new SerializableDictionary<MaterialSheet, int>();

		private PowerState _currentPowerState = PowerState.Off;

		public MaterialStorage MaterialStorage => materialStorageLink.usedStorage;

		private int _maniTier = 1;
		private float ProductionTime => Math.Max(0f, productionTimeBase - ((_maniTier-1) * 3f)); //From 10s to 1s

		private int _binTier = 1;
		private float Discount => Math.Max(0.5f, 1 - ((_binTier - 1) * 0.15f)); //From 100% to 55% resource usage

		private bool _isProducing = false;

		[SerializeField, Tooltip("The time to produce a flatpack in seconds with basic lasers")] private float productionTimeBase = 10f;

		private void Start()
		{
			_loadedMachineBoardSlot = loadedItemStorage.GetIndexedItemSlot(0);
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			CancelProduction();
			if (materialStorageLink != null)
			{
				materialStorageLink.Despawn();
			}
		}

		public void UpdateGUI()
		{
			// Delegate calls method in all subscribers when material is changed
			MaterialsManipulated?.Invoke();
			if(_loadedMachineBoard == null) OnMachineChange?.Invoke(null,null,null );
			else OnMachineChange?.Invoke(_loadedMachineBoard.MachinePartsUsed.machine.ExpensiveName(), _neededMaterials, MaterialStorage.MaterialList);
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (_isProducing) return false;

			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.TargetObject != gameObject) return false;

			if(interaction.HandObject == false) return true; //Open UI

			if(_loadedMachineBoardSlot.IsEmpty && Validations.HasItemTrait(interaction.UsedObject, machineBoardTrait)) return true; //Load Board

			_insertedMaterialType = materialStorageLink.usedStorage.FindMaterial(interaction.HandObject); //Load mats
			return _insertedMaterialType == true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (_currentPowerState == PowerState.Off) Chat.AddExamineMsgFromServer(interaction.Performer, $"{gameObject.ExpensiveName()} is unpowered!");
			if (_isProducing || _currentPowerState != PowerState.On) return;

			if (interaction.HandObject == false) TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType.Flatpacker, TabAction.Open );
			else if (_loadedMachineBoardSlot.IsEmpty && Validations.HasItemTrait(interaction.UsedObject, machineBoardTrait))
			{
				if (interaction.HandObject.TryGetComponent<MachineCircuitBoard>(out var board) == false) return;
				_loadedMachineBoard = board;
				Inventory.ServerTransfer(interaction.HandSlot, _loadedMachineBoardSlot);
				Chat.AddActionMsgToChat(interaction.Performer, $"You load the {interaction.HandObject.ExpensiveName()} into the flatpacker.",
					$"{interaction.Performer.ExpensiveName()} loads the {interaction.HandObject.ExpensiveName()} into the flatpacker.");

				UpdateCurrentMachine();

				return;
			}

			_insertedMaterialType = materialStorageLink.usedStorage.FindMaterial(interaction.HandObject);
			if (_insertedMaterialType == true)
			{
				int materialSheetAmount = interaction.HandSlot.Item.GetComponent<Stackable>().Amount;
				if (materialStorageLink.TryAddSheet(_insertedMaterialType, materialSheetAmount))
				{
					interaction.HandSlot.Item.GetComponent<Stackable>().ServerConsume(materialSheetAmount);
					UpdateGUI();
				}
				else Chat.AddActionMsgToChat(interaction.Performer, "Flatpacker is full",
					"Flatpacker is full");
			}
		}

		public void DispenseMaterialSheet(int amountOfSheets, ItemTrait materialType)
		{
			materialStorageLink.usedStorage.DispenseSheet(amountOfSheets, materialType, gameObject.AssumedWorldPosServer());
			UpdateGUI();
		}


		#region Production

		private const int ON_OVERLAY_VARIANT = 1;
		private const int OFF_OVERLAY_VARIANT = 0;

		private const int EJECTING_PRIMARY_VARIANT = 1;
		private const int NETURAL_PRIMARY_VARIANT = 0;

		private Coroutine _productionCoroutine = null;

		private void EjectFlatpack()
		{
			GameObject flatPackObject = Spawn.ServerPrefab(flatPackPrefab, objectPhysics.OfficialPosition).GameObject;
			if(flatPackObject.TryGetComponent<Flatpack>(out var flatpack) == false) return;

			string machineName = _loadedMachineBoard.MachinePartsUsed.machine.ExpensiveName();
			flatpack.InitialiseType(_loadedMachineBoard.relevantDepartment, machineName);

			SpawnParts(flatpack);
			SpawnRawMaterials(flatpack);
			PackMachineBoard(flatpack);

			materialStorageLink.usedStorage.TryConsumeList(_neededMaterials);

			UpdateGUI();
		}

		private void SpawnParts(Flatpack flatpack)
		{
			List<GameObject> objectsToStore = new List<GameObject>();
			foreach (var toSpawn in _partsToSpawn)
			{
				objectsToStore.Add(Spawn
					.ServerPrefab(CustomNetworkManager.Instance.ForeverIDLookupSpawnablePrefabs[toSpawn],
						objectPhysics.OfficialPosition).GameObject);
			}

			flatpack.objectContainer.StoreObjects(objectsToStore);
		}

		private void SpawnRawMaterials(Flatpack flatpack)
		{
			Spawn.ServerPrefab(metalSheetPrefab, objectPhysics.OfficialPosition, count: 5);
			Spawn.ServerPrefab(cableCoilPrefab, objectPhysics.OfficialPosition, count: 5);
			flatpack.objectContainer.GatherObjects();
		}

		private void PackMachineBoard(Flatpack flatpack)
		{
			loadedItemStorage.ServerTryRemove(_loadedMachineBoardSlot.ItemObject);
			flatpack.objectContainer.StoreObject(_loadedMachineBoard.gameObject);

			_loadedMachineBoard = null;
		}

		private void CancelProduction()
		{
			primarySpriteHandler.SetSpriteVariant(NETURAL_PRIMARY_VARIANT);
			overlaySpriteHandler.SetSpriteVariant(OFF_OVERLAY_VARIANT);

			if(_productionCoroutine != null) StopCoroutine(_productionCoroutine);
			_productionCoroutine = null;
		}

		public void BeginProduction()
		{
			if (_productionCoroutine != null) return;
			_productionCoroutine = StartCoroutine(AnimateProduction());
		}

		private IEnumerator AnimateProduction()
		{
			_isProducing = true;
			_ = SoundManager.PlayNetworkedAtPosAsync(beginSound, objectPhysics.OfficialPosition);
			_ = SoundManager.PlayNetworkedAtPosAsync(processingSound, objectPhysics.OfficialPosition);
			overlaySpriteHandler.SetSpriteVariant(ON_OVERLAY_VARIANT);

			yield return WaitFor.Seconds(ProductionTime);

			primarySpriteHandler.SetSpriteVariant(EJECTING_PRIMARY_VARIANT);
			overlaySpriteHandler.SetSpriteVariant(OFF_OVERLAY_VARIANT);
			_ = SoundManager.PlayNetworkedAtPosAsync(finishSound, objectPhysics.OfficialPosition);

			yield return WaitFor.Seconds(0.5f);

			primarySpriteHandler.SetSpriteVariant(NETURAL_PRIMARY_VARIANT);
			EjectFlatpack();
			_isProducing = false;
			_productionCoroutine = null;
		}

		#endregion
		public void EjectMachineBoard()
		{
			loadedItemStorage.DropObjects();
			_loadedMachineBoard = null;
			UpdateCurrentMachine();

			CancelProduction();
		}

		#region GeneratingFlatpackData

		private void UpdateCurrentMachine(GameObject performer = null)
		{
			_neededMaterials.Clear();
			_partsToSpawn.Clear();

			if (_loadedMachineBoard == null)
			{
				UpdateGUI();
				return;
			}

			foreach (var part in _loadedMachineBoard.MachinePartsUsed.machineParts)
			{
				Design designToAdd = null;
				if (designProductionData.TraitsToDesignID.ContainsKey(part.itemTrait))
				{
					string designID = designProductionData.TraitsToDesignID[part.itemTrait];
					if(Designs.Globals.InternalIDSearch.ContainsKey(designID)) designToAdd = Designs.Globals.InternalIDSearch[designID];
				}

				if (designToAdd == null)
				{
					if(performer != null) Chat.AddWarningMsgFromServer(performer, $"Flatpacker unable to find suitable design for: {part.itemTrait}, will not be added to produced flatpack");
					continue;
				}

				AddCostsForDesign(designToAdd.Materials, part.amountOfThisPart);
				for(int i = 0; i < part.amountOfThisPart; i++) _partsToSpawn.Add(designToAdd.ItemID);
			}

			UpdateGUI();
		}

		private void AddCostsForDesign(Dictionary<string, int> materialCosts, int amountOfParts)
		{
			var sheet = designProductionData.MaterialSheets["Metal"]; //Add base metal and glass costs for the metal and cables needed in frame
			_neededMaterials.TryAdd(sheet, 0);
			_neededMaterials[sheet] += (5 * 2000) + 500; //We don't discount this, as discounting raw mats can lead to dupes

			sheet = designProductionData.MaterialSheets["Glass"];
			_neededMaterials.TryAdd(sheet, 0);
			_neededMaterials[sheet] += 200;

			foreach (var material in materialCosts) //Add design costs
			{
				sheet = designProductionData.MaterialSheets[material.Key];
				_neededMaterials.TryAdd(sheet, 0);
				_neededMaterials[sheet] += (int)(material.Value * amountOfParts * Discount);
			}


		}

		#endregion

		#region IRefreshParts

		public void RefreshParts(IDictionary<PartReference, int> partsInFrame, Machine Frame)
		{
			_maniTier = 0;
			// Get the machine stock parts used in this instance and get the tier of each part.
			// Collection is unorganized so run through the whole list.
			foreach (var part in partsInFrame.Keys)
			{
				if (part.itemTrait == MachinePartsItemTraits.Instance.Manipulator) _maniTier += part.tier / 2;
				if (part.itemTrait == MachinePartsItemTraits.Instance.MatterBin) _binTier = part.tier;
			}
		}

		#endregion

		#region IAPCPowerable

		public void PowerNetworkUpdate(float voltage)
		{
			// Nothing really.  Only the state matters.  (See StateUpdate).
		}

		public void StateUpdate(PowerState State)
		{
			_currentPowerState = State;
			if (overlaySpriteHandler == null || primarySpriteHandler == null) return;

			if (State != PowerState.On)
			{
				overlaySpriteHandler.SetCatalogueIndexSprite(1);
				TabUpdateMessage.SendToPeepers(gameObject, NetTabType.Flatpacker, TabAction.Close);

				CancelProduction();
			} else overlaySpriteHandler.SetCatalogueIndexSprite(0);
		}

		#endregion

	}
}
