using SecureStuff;
using UnityEngine;
using UnityEngine.Serialization;
using US13.Core.ObjectConnection;
using US13.Items.Traits;
using US13.Objects.Mining;
using US13.Systems.Construction;
using US13.UI.Objects.Mining.Ore_Redemption_Machine;

namespace US13.Objects.Machines
{
	/// <summary>
	/// links machines with their own material storage or material silo
	/// </summary>
	public class MaterialStorageLink : MonoBehaviour,  IMultitoolSlaveable
	{
		[FormerlySerializedAs("IsUsingSilo")]
		public bool InitialIsUsingSilo;
		[PlayModeOnly] public bool IsUsingSilo;


		[FormerlySerializedAs("usedStorage")]
		public MaterialStorage InitialusedStorage;
		[PlayModeOnly] public MaterialStorage usedStorage;

		private MaterialStorage selfStorage;
		public GUI_MaterialsList materialListGUI;

		public IMultitoolMasterable Master { set; get; }
		public bool RequireLink => false;

		public MultitoolConnectionType ConType => MultitoolConnectionType.OreSilo;

		public bool CanRelink => true;

		public bool TrySetMaster(GameObject performer, IMultitoolMasterable master)
		{
			if (master is not MaterialSilo Silo) return false;

			Master = Silo;
			ConnectToSilo(Silo.materialStorage);
			return true;
		}

		public void SetMasterEditor(IMultitoolMasterable master)
		{
			if (master is not MaterialSilo Silo) return;
			Master = Silo;
			ConnectToSilo(Silo.materialStorage);
		}

		private void Awake()
		{
			IsUsingSilo = InitialIsUsingSilo;
			usedStorage = InitialusedStorage;
			selfStorage = GetComponent<MaterialStorage>();
			usedStorage = selfStorage;
			usedStorage.UpdateGUIs.AddListener(UpdateGUI);
		}


		public bool CanFit(MaterialMakeUp MaterialMakeUp, int Stackquantity)
		{
			return usedStorage.CanFit(MaterialMakeUp, Stackquantity);
		}

		public void AddMaterial(ItemTrait material, int quantity)
		{
			usedStorage.AddMaterial(material, quantity);
		}

		public bool TryAddSheet(ItemTrait InsertedMaterialType, int materialSheetAmount)
		{
			return usedStorage.TryAddSheet(InsertedMaterialType, materialSheetAmount);
		}

		public void ConnectToSilo(MaterialStorage silo)
		{
			if (!IsUsingSilo)
			{
				usedStorage.UpdateGUIs.RemoveListener(UpdateGUI);
				usedStorage = silo;
				IsUsingSilo = true;
				usedStorage.UpdateGUIs.AddListener(UpdateGUI);
				UpdateGUI();
			}
		}

		public void DisconnectFromSilo()
		{
			usedStorage.UpdateGUIs.RemoveListener(UpdateGUI);
			usedStorage = selfStorage;
			IsUsingSilo = false;
			usedStorage.UpdateGUIs.AddListener(UpdateGUI);
			UpdateGUI();
		}

		public void Despawn()
		{
			if (IsUsingSilo)
				return;
			usedStorage.DropAllMaterials();
		}

		public void UpdateGUI()
		{
			if (materialListGUI)
			{
				materialListGUI.UpdateMaterialList();
			}
		}
	}
}
