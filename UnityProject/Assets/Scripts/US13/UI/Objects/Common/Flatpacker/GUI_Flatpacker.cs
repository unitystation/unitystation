using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using US13.Items;
using US13.Items.Traits;
using US13.Managers;
using US13.ScriptableObjects.Research;
using US13.UI.Core.Net;
using US13.UI.Core.Net.Page;

namespace US13.UI.Objects.Common.Flatpacker
{
	public class GUI_Flatpacker : NetTab
	{
		public US13.Objects.Machines.Flatpacker provider { get; private set; }

		[field: SerializeField] public DesignProductionData productionData { get; private set; }
		[SerializeField] private NetPageSwitcher pageSwitcher = null;

		[SerializeField] private NetPage emptyPage = null;
		[SerializeField] private GUI_FlatpackerMachinePage machinePage = null;
		[SerializeField] private GUI_MaterialsPage materialsPage = null;

		private bool isUpdating = false;
		public bool hasFunds = false;

		private void Start()
		{
			machinePage.SetMaster(this);
		}

		protected override void InitServer()
		{
			StartCoroutine(WaitForProvider());
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}
			provider = Provider.GetComponentInChildren<US13.Objects.Machines.Flatpacker>();

			US13.Objects.Machines.Flatpacker.MaterialsManipulated += UpdateMaterialsPage;
			provider.OnMachineChange += UpdateGUI;

			materialsPage.InitMaterialList(provider.MaterialStorage);
			OnTabOpened.AddListener(UpdateGUIForPeepers);

			provider.UpdateGUI();
		}

		public void UpdateGUIForPeepers(PlayerInfo notUsed)
		{
			if (!isUpdating)
			{
				isUpdating = true;
				StartCoroutine(WaitForClient());
			}
		}

		private IEnumerator WaitForClient()
		{
			yield return WaitFor.Seconds(0.2f);
			materialsPage.UpdateMaterialList(provider.MaterialStorage);
			isUpdating = false;
		}

		public void UpdateGUI(string machineName, SerializableDictionary<MaterialSheet, int> neededMaterials,
			Dictionary<ItemTrait, int> currentMaterials)
		{
			if (machineName == null)
			{
				hasFunds = false;
				pageSwitcher.SetActivePage(emptyPage);
			}
			else
			{
				hasFunds = true;
				if(pageSwitcher.CurrentPage != machinePage) pageSwitcher.SetActivePage(machinePage);
				machinePage.UpdateText(machineName, neededMaterials, currentMaterials, ref hasFunds);
			}
		}

		public void UpdateMaterialsPage()
		{
			materialsPage.UpdateMaterialList(provider.MaterialStorage);
		}

		public void OnDispenseItemButtonPressed(int amount, ItemTrait materialType)
		{
			provider.DispenseMaterialSheet(amount, materialType);
		}

		private void OnDestroy()
		{
			US13.Objects.Machines.Flatpacker.MaterialsManipulated -= UpdateMaterialsPage;
			provider.OnMachineChange -= UpdateGUI;
		}
	}
}
