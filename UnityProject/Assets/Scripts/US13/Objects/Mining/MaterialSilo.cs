using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.Core.ObjectConnection;
using US13.Items;
using US13.Items.Traits;
using US13.Managers;
using US13.Managers.NetworkManagement;
using US13.Objects.Machines;
using US13.Systems.Construction;
using US13.Systems.Inventory;
using US13.Tilemaps.Behaviours.Objects;

namespace US13.Objects.Mining
{
	public class MaterialSilo : MonoBehaviour, ICheckedInteractable<HandApply>, IServerLifecycle, IMultitoolMasterable
	{
		[FormerlySerializedAs("linkedStorages")]
		public List<MaterialStorageLink> InitiallinkedStorages = new List<MaterialStorageLink>();
		private List<MaterialStorageLink> linkedStorages = new List<MaterialStorageLink>();

		private ItemTrait InsertedMaterialType;
		public MaterialStorage materialStorage;

		private bool MapSpawned = false;


		public bool IgnoreMaxDistanceMapper { get; set; } = true;
		public int MaxDistance { get; set; } = 999;
		public bool CanBeMastered { get; set; } = false;

		public MultitoolConnectionType ConType => MultitoolConnectionType.OreSilo;

		public bool CanRelink => true;

		private void Awake()
		{
			linkedStorages.AddRange(InitiallinkedStorages);
			materialStorage = GetComponent<MaterialStorage>();
			CraftingManager.RoundstartStationSilo = this;
		}

		public void Start()
		{
			if (CustomNetworkManager.IsServer == false) return;
			if (MapSpawned == false) return;


			var registerTile = GetComponent<RegisterTile>();
			var array = registerTile.Matrix.GetComponentsInChildren<MaterialStorageLink>();
			foreach (var otherStorage in array)
			{
				var registerObject = otherStorage.GetComponent<RegisterObject>();
				if (registerObject.Matrix.IsMainStation)
				{
					otherStorage.ConnectToSilo(materialStorage);
					linkedStorages.Add(otherStorage);
				}
			}
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			MapSpawned = info.WasMapspawn;

		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side))
				return false;

			if (Validations.HasComponent<MaterialMakeUp>(interaction.HandObject))
			{
				return true;
			}

			InsertedMaterialType = materialStorage.FindMaterial(interaction.HandObject);
			if (InsertedMaterialType != null)
			{
				return true;
			}
			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			var MaterialMakeUp = interaction.HandObject.GetComponent<MaterialMakeUp>();
			var stackable = interaction.HandObject.GetComponent<Stackable>();
			if (MaterialMakeUp != null)
			{
				var StackableAmount = 1;
				if (stackable != null)
				{
					StackableAmount = stackable.Amount;
				}

				if (materialStorage.CanFit(MaterialMakeUp, StackableAmount))
				{
					foreach (var Material in MaterialMakeUp.MakeUp)
					{
						materialStorage.AddMaterial(Material.Key.materialTrait, Material.Value * StackableAmount);
					}
					_ = Inventory.ServerDespawn(interaction.HandObject);
				}
			}
			else
			{
				var canadd = materialStorage.TryAddSheet(InsertedMaterialType, stackable.Amount);
				if (canadd)
				{
					_ = Inventory.ServerDespawn(interaction.HandObject);
				}
			}

		}

		public void OnDespawnServer(DespawnInfo info)
		{
			materialStorage.DropAllMaterials();
			foreach (var linkedMat in linkedStorages)
			{
				if (linkedMat == null) //they were destroyed at some point, irrelevant to us
				{
					continue;
				}
				linkedMat.DisconnectFromSilo();
			}
		}
	}
}