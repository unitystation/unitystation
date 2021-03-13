using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Objects.Machines
{
	/// <summary>
	/// links machines with their own material storage or material silo
	/// </summary>
	public class MaterialStorageLink : MonoBehaviour
	{
		private bool IsUsingSilo;
		public MaterialStorage usedStorage;
		private MaterialStorage selfStorage;


		private void Awake()
		{
			selfStorage = GetComponent<MaterialStorage>();
			usedStorage = selfStorage;
		}

		public bool TryAddSheet(ItemTrait InsertedMaterialType, int materialSheetAmount)
		{
			return usedStorage.TryAddSheet(InsertedMaterialType, materialSheetAmount);
		}


		public void ConnectToSilo(MaterialStorage silo)
		{
			usedStorage = silo;
			IsUsingSilo = true;
		}

		public void DisconnectFromSilo()
		{
			usedStorage = selfStorage;
			IsUsingSilo = false;
		}

		public void Despawn()
		{
			if (IsUsingSilo)
				return;
			usedStorage.DropAllMaterials();
		}
	}
}
