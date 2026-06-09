using System.Collections;
using UnityEngine;
using US13.Managers;
using US13.Objects.Mining;
using US13.UI.Core.Net;
using US13.UI.Core.Net.Elements;

namespace US13.UI.Objects.Mining.Ore_Redemption_Machine
{
	public class GUI_OreRedemptionMachine : NetTab
	{
		[SerializeField]
		private GUI_MaterialsList materialsListDisplay = null;

		private OreRedemptionMachine oreRedemptionMachine;
		private bool loadOresCooldown;

		public NetText_label laborPointsLabel;
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

			oreRedemptionMachine = Provider.GetComponent<OreRedemptionMachine>();
			UpdateLaborPoints(oreRedemptionMachine.laborPoints);
			oreRedemptionMachine.oreRedemptiomMachineGUI = this;
			materialsListDisplay.materialStorageLink = oreRedemptionMachine.materialStorageLink;
			materialsListDisplay.materialStorageLink.materialListGUI = materialsListDisplay;
			materialsListDisplay.UpdateMaterialList();
		}

		public void UpdateLaborPoints(int laborPoints)
		{
			laborPointsLabel.MasterSetValue($"Unclaimed points: {laborPoints.ToString()}");
		}

		public void ClaimLaborPoints(PlayerInfo connectedPlayer)
		{
			oreRedemptionMachine.ClaimLaborPoints(connectedPlayer.GameObject);
		}

		public void LoadNearbyOres()
		{
			if (!loadOresCooldown)
			{
				oreRedemptionMachine.LoadNearbyOres();
				StartCoroutine(LoadOresCooldown());
			}
		}

		private IEnumerator LoadOresCooldown()
		{
			loadOresCooldown = true;
			yield return new WaitForSeconds(2);
			loadOresCooldown = false;
		}
	}
}
