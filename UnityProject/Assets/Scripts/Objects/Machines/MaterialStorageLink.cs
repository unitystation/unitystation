using System;
using System.Collections.Generic;
using UnityEngine;
using UI.Objects.Cargo;

namespace Objects.Machines
{
	/// <summary>
	/// links machines with their own material storage or material silo
	/// </summary>
	public class MaterialStorageLink : MonoBehaviour
	{
		public bool IsUsingSilo;
		public MaterialStorage usedStorage;
		private MaterialStorage selfStorage;
		public GUI_MaterialsList materialListGUI;

		private void Awake()
		{
			selfStorage = GetComponent<MaterialStorage>();
			usedStorage = selfStorage;
			usedStorage.UpdateGUIs.AddListener(UpdateGUI);
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
