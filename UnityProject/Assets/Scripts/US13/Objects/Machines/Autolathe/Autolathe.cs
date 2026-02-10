using System.Collections;
using Mirror;
using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.Core.Sprite_Handler;
using US13.Items;
using US13.Items.Traits;
using US13.Objects.Engineering;
using US13.Systems.Construction;
using US13.Systems.Electricity.Interfaces;
using US13.Systems.Inventory;
using US13.Tilemaps.Behaviours.Objects;
using Util;

namespace US13.Objects.Machines.Autolathe
{
	[RequireComponent(typeof(MaterialStorageLink))]
	public class Autolathe : NetworkBehaviour, ICheckedInteractable<HandApply>, IServerSpawn, IServerDespawn,
		IAPCPowerable
	{
		public PowerState PoweredState;

		[SyncVar(hook = nameof(SyncSprite))] private AutolatheState stateSync;

		[SerializeField] private SpriteHandler spriteHandler;

		[SerializeField] private SpriteDataSO idleSprite;

		[SerializeField] private SpriteDataSO productionSprite;

		[SerializeField] private SpriteDataSO acceptingMaterialsSprite;

		private RegisterObject registerObject;

		public MaterialStorageLink materialStorageLink;

		[SerializeField] private MachineProductsCollection autolatheProducts;

		public MachineProductsCollection AutolatheProducts
		{
			get => autolatheProducts;
		}

		public delegate void MaterialsManipulating();

		public static event MaterialsManipulating MaterialsManipulated;

		private ItemTrait InsertedMaterialType;
		private IEnumerator currentProduction;

		private Machine machine;

		public enum AutolatheState
		{
			Idle,
			AcceptingMaterials,
			Production,
		}

		#region Lifecycle

		public void Awake()
		{
			registerObject = GetComponent<RegisterObject>();
			materialStorageLink = GetComponent<MaterialStorageLink>();
			machine = GetComponent<Machine>();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			SyncSprite(AutolatheState.Idle, AutolatheState.Idle);
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

		public void UpdateGUI()
		{
			// Delegate calls method in all subscribers when material is changed
			MaterialsManipulated?.Invoke();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (interaction.HandSlot.IsEmpty) return false;
			if (DefaultWillInteract.Default(interaction, side) == false) return false;


			if (Validations.HasComponent<MaterialMakeUp>(interaction.HandObject))
			{
				return true;
			}

			InsertedMaterialType = materialStorageLink.usedStorage.FindMaterial(interaction.HandObject);
			if (InsertedMaterialType != null)
			{
				return true;
			}

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			// Can't insert materials while exofab is in production.
			if (stateSync != AutolatheState.Production)
			{
				var MaterialMakeUp = interaction.HandObject.GetComponent<MaterialMakeUp>();
				var stackable = interaction.HandObject.GetComponent<Stackable>();
				bool adding = false;

				if (MaterialMakeUp != null)
				{
					var StackableAmount = 1;
					if (stackable != null)
					{
						StackableAmount = stackable.Amount;
					}

					if (materialStorageLink.CanFit(MaterialMakeUp, StackableAmount))
					{
						foreach (var Material in MaterialMakeUp.MakeUp)
						{
							materialStorageLink.AddMaterial(Material.Key.materialTrait,
								Material.Value * StackableAmount);
						}

						_ = Inventory.ServerDespawn(interaction.HandObject);
						adding = true;
					}
				}
				else
				{
					var canadd = materialStorageLink.TryAddSheet(InsertedMaterialType, stackable.Amount);
					if (canadd)
					{
						_ = Inventory.ServerDespawn(interaction.HandObject);
						adding = true;
					}
				}

				if (adding)
				{
					if (stateSync == AutolatheState.Idle)
					{
						StartCoroutine(AnimateAcceptingMaterials());
					}
				}
				else
					Chat.AddActionMsgToChat(interaction.Performer, "Autolathe is full",
						"Autolathe is full");
			}
			else
				Chat.AddActionMsgToChat(interaction.Performer, "Cannot accept materials while fabricating",
					"Cannot accept materials while fabricating");
		}

		private IEnumerator AnimateAcceptingMaterials()
		{
			stateSync = AutolatheState.AcceptingMaterials;

			yield return WaitFor.Seconds(0.9f);
			if (stateSync == AutolatheState.Production)
			{
				// Do nothing if production was started during the material insertion animation
			}
			else
			{
				stateSync = AutolatheState.Idle;
			}

			UpdateGUI();
		}

		public void DispenseMaterialSheet(int amountOfSheets, ItemTrait materialType)
		{
			materialStorageLink.usedStorage.DispenseSheet(amountOfSheets, materialType,
				gameObject.AssumedWorldPosServer());
			UpdateGUI();
		}

		[Server]
		public bool CanProcessProduct(MachineProduct product)
		{
			if (materialStorageLink.usedStorage.TryConsumeList(product.materialToAmounts,
				    0.5f / (machine.GetPartMultiplier() / 2f)))
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
			UpdateGUI();
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

		#region IAPCPowerable

		public void PowerNetworkUpdate(float voltage)
		{
		}

		public void StateUpdate(PowerState state)
		{
			PoweredState = state;
		}

		#endregion
	}
}