using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Objects.Machines;

namespace UI.Objects.Cargo
{
	public class GUI_MaterialSilo : NetTab
	{
		[SerializeField]
		private GUI_MaterialsList materialsListDisplay = null;

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
			materialsListDisplay.materialStorageLink = Provider.GetComponent<MaterialStorageLink>();
			materialsListDisplay.materialStorageLink.materialListGUI = materialsListDisplay;
			materialsListDisplay.UpdateMaterialList();
		}
	}
}
