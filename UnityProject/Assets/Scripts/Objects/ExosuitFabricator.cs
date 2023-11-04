using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Objects.Machines;

namespace Objects.Robotics
{
	/// <summary>
	/// Main component for the exosuit fabricator.
	/// </summary>
	public class ExosuitFabricator : NetworkBehaviour, ICheckedInteractable<HandApply>, IServerSpawn, IServerDespawn
	{
		[SyncVar(hook = nameof(SyncSprite))]
		private ExosuitFabricatorState stateSync;

		private SpriteHandler spriteHandler;
		[SerializeField] private SpriteDataSO idleSprite;
		[SerializeField] private SpriteDataSO acceptingMaterialsSprite;
		[SerializeField] private SpriteDataSO productionSprite;
		private RegisterObject registerObject;
		public MaterialStorageLink materialStorageLink;
		public MachineProductsCollection exoFabProducts;
		private ItemTrait InsertedMaterialType;
		private IEnumerator currentProduction;

		public delegate void MaterialsManipulating();

		public static event MaterialsManipulating MaterialsManipulated;

		private void UpdateGUI()
		{
			//Delegate calls method in all subscribers when material is changed
			if (MaterialsManipulated != null)
			{
				MaterialsManipulated();
			}
		}

		public enum ExosuitFabricatorState
		{
			Idle,
			AcceptingMaterials,
			Production,
		};

		public void OnSpawnServer(SpawnInfo info)
		{
			SyncSprite(ExosuitFabricatorState.Idle, ExosuitFabricatorState.Idle);
		}

		private void Awake()
		{
			registerObject = GetComponent<RegisterObject>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			materialStorageLink = GetComponent<MaterialStorageLink>();
		}

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

		//Clicking the exofab with material sheets(Metal sheets, glass sheets, silver sheets, etc.)
		//in hand will insert the materials in the storage and update the GUI.
		//Every sheet is 2000cm^3

		public void ServerPerformInteraction(HandApply interaction)
		{
			//Can't insert materials while exofab is in production.
			if (stateSync != ExosuitFabricatorState.Production)
			{
				int materialSheetAmount = interaction.HandSlot.Item.GetComponent<Stackable>().Amount;
				if (materialStorageLink.usedStorage.TryAddSheet(InsertedMaterialType, materialSheetAmount))
				{
					_ = Inventory.ServerDespawn(interaction.HandObject);
					if (stateSync == ExosuitFabricatorState.Idle)
					{
						StartCoroutine(AnimateAcceptingMaterials());
					}
					UpdateGUI();
				}
				else Chat.AddActionMsgToChat(interaction.Performer, "Exosuit Fabricator is full",
					"Exosuit Fabricator is full");
			}
			else Chat.AddActionMsgToChat(interaction.Performer, "Cannot accept materials while fabricating",
				"Cannot accept materials while fabricating");
		}

		private IEnumerator AnimateAcceptingMaterials()
		{
			stateSync = ExosuitFabricatorState.AcceptingMaterials;

			yield return WaitFor.Seconds(1.2f);
			if (stateSync == ExosuitFabricatorState.Production)
			{
				//Do nothing if production was started during the material insertion animation
			}
			else
			{
				stateSync = ExosuitFabricatorState.Idle;
			}
		}

		public void DispenseMaterialSheet(int amountOfSheets, ItemTrait materialType)
		{
			materialStorageLink.usedStorage.DispenseSheet(amountOfSheets, materialType, gameObject.AssumedWorldPosServer());
			UpdateGUI();
		}

		/// <summary>
		/// Checks the material storage to see if there's enough materials and if true will process the product
		/// </summary>
		public bool CanProcessProduct(MachineProduct product)
		{
			if (materialStorageLink.usedStorage.TryConsumeList(product.materialToAmounts))
			{
				currentProduction = ProcessProduction(product.Product, product.ProductionTime);
				StartCoroutine(currentProduction);
				return true;
			}

			return false;
		}

		private IEnumerator ProcessProduction(GameObject productObject, float productionTime)
		{
			stateSync = ExosuitFabricatorState.Production;
			yield return WaitFor.Seconds(productionTime);

			Spawn.ServerPrefab(productObject, registerObject.WorldPositionServer + Vector3Int.down, transform.parent, count: 1);
			stateSync = ExosuitFabricatorState.Idle;
		}

		public void SyncSprite(ExosuitFabricatorState stateOld, ExosuitFabricatorState stateNew)
		{
			stateSync = stateNew;
			if (stateNew == ExosuitFabricatorState.Idle)
			{
				spriteHandler.SetSpriteSO(idleSprite);
			}
			else if (stateNew == ExosuitFabricatorState.Production)
			{
				spriteHandler.SetSpriteSO(productionSprite);
			}
			else if (stateNew == ExosuitFabricatorState.AcceptingMaterials)
			{
				spriteHandler.SetSpriteSO(acceptingMaterialsSprite);
			}
			else
			{
				//Do nothing
			}
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			if (currentProduction != null)
			{
				StopCoroutine(currentProduction);
				currentProduction = null;
			}

			materialStorageLink.usedStorage.DropAllMaterials();
		}
	}
}
