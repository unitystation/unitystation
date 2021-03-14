﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Systems.Electricity;

namespace Objects.Machines
{
	public class Autolathe : NetworkBehaviour, ICheckedInteractable<HandApply>, IServerSpawn, IServerDespawn, IAPCPowered
	{

		public PowerStates PoweredState;

		[SyncVar(hook = nameof(SyncSprite))]
		private AutolatheState stateSync;

		[SerializeField]
		private SpriteHandler spriteHandler;

		[SerializeField]
		private SpriteDataSO idleSprite;

		[SerializeField]
		private SpriteDataSO productionSprite;

		[SerializeField]
		private SpriteDataSO acceptingMaterialsSprite;

		private RegisterObject registerObject;

		public MaterialStorageLink materialStorageLink;

		[SerializeField]
		private MachineProductsCollection autolatheProducts;

		public MachineProductsCollection AutolatheProducts { get => autolatheProducts; }

		public delegate void MaterialsManipulating();

		public static event MaterialsManipulating MaterialsManipulated;

		private ItemTrait InsertedMaterialType;
		private IEnumerator currentProduction;

		public enum AutolatheState
		{
			Idle,
			AcceptingMaterials,
			Production,
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			SyncSprite(AutolatheState.Idle, AutolatheState.Idle);
		}

		public void Awake()
		{
			registerObject = GetComponent<RegisterObject>();
			materialStorageLink = GetComponent<MaterialStorageLink>();
		}

		private void UpdateGUI()
		{
			//Delegate calls method in all subscribers when material is changed
			if (MaterialsManipulated != null)
			{
				MaterialsManipulated();
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (interaction.HandSlot.IsEmpty) return false;
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			InsertedMaterialType = materialStorageLink.usedStorage.FindMaterial(interaction.HandObject);
			if (InsertedMaterialType != null)
			{
				return true;
			}
			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			//Can't insert materials while exofab is in production.
			if (stateSync != AutolatheState.Production)
			{
				int materialSheetAmount = interaction.HandSlot.Item.GetComponent<Stackable>().Amount;
				if (materialStorageLink.TryAddSheet(InsertedMaterialType, materialSheetAmount))
				{
					Inventory.ServerDespawn(interaction.HandObject);
					if (stateSync == AutolatheState.Idle)
					{
						StartCoroutine(AnimateAcceptingMaterials());
					}
					UpdateGUI();
				}
				else Chat.AddActionMsgToChat(interaction.Performer, "Autolathe is full",
					"Autolathe is full");
			}
			else Chat.AddActionMsgToChat(interaction.Performer, "Cannot accept materials while fabricating",
				"Cannot accept materials while fabricating");
		}

		private IEnumerator AnimateAcceptingMaterials()
		{
			stateSync = AutolatheState.AcceptingMaterials;

			yield return WaitFor.Seconds(0.9f);
			if (stateSync == AutolatheState.Production)
			{
				//Do nothing if production was started during the material insertion animation
			}
			else
			{
				stateSync = AutolatheState.Idle;
			}
		}

		public void DispenseMaterialSheet(int amountOfSheets, ItemTrait materialType)
		{
			materialStorageLink.usedStorage.DispenseSheet(amountOfSheets, materialType, gameObject.WorldPosServer());
			UpdateGUI();
		}

		[Server]
		public bool CanProcessProduct(MachineProduct product)
		{
			if (materialStorageLink.usedStorage.TryConsumeList(product.materialToAmounts))
			{
				if (APCPoweredDevice.IsOn(PoweredState))
				{
					currentProduction = ProcessProduction(product.Product, product.ProductionTime);
					StartCoroutine(currentProduction);
					return true;
				}
			}

			return false;
		}

		private IEnumerator ProcessProduction(GameObject productObject, float productionTime)
		{
			stateSync = AutolatheState.Production;
			yield return WaitFor.Seconds(productionTime);

			Spawn.ServerPrefab(productObject, registerObject.WorldPositionServer, transform.parent, count: 1);
			stateSync = AutolatheState.Idle;
		}

		public void SyncSprite(AutolatheState stateOld, AutolatheState stateNew)
		{
			stateSync = stateNew;
			if (stateNew == AutolatheState.Idle)
			{
				spriteHandler.SetSpriteSO(idleSprite);
			}
			else if (stateNew == AutolatheState.Production)
			{
				spriteHandler.SetSpriteSO(productionSprite);
			}
			else if (stateNew == AutolatheState.AcceptingMaterials)
			{
				spriteHandler.SetSpriteSO(acceptingMaterialsSprite);
			}
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			if (currentProduction != null)
			{
				StopCoroutine(currentProduction);
				currentProduction = null;
			}
			materialStorageLink.Despawn();
		}

		public void PowerNetworkUpdate(float Voltage) { }

		public void StateUpdate(PowerStates State)
		{
			PoweredState = State;
		}
	}
}
