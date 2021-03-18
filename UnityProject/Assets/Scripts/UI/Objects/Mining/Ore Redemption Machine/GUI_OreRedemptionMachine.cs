using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Objects.Machines;

namespace Objects.Mining
{
	public class GUI_OreRedemptionMachine : NetTab
	{
		[SerializeField]
		private GUI_MaterialsList materialsListDisplay = null;

		private OreRedemptionMachine oreRedemptionMachine;

		public NetLabel laborPointsLabel;
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
			laborPointsLabel.SetValueServer($"Unclaimed points: {laborPoints.ToString()}");
		}

		public void ClaimLaborPoints(ConnectedPlayer connectedPlayer)
		{
			oreRedemptionMachine.ClaimLaborPoints(connectedPlayer.GameObject);
		}
	}
}
