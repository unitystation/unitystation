using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UI.Core.NetUI;
using Objects.Machines;

namespace UI.Objects.Robotics
{
	public class GUI_ExoFabItem : DynamicEntry
	{
		private GUI_ExosuitFabricator ExoFabMasterTab = null;
		private MachineProduct product = null;

		public MachineProduct Product {
			get => product;
			set {
				product = value;
				ReInit();
			}
		}

		public void AddToQueue()
		{
			if (ExoFabMasterTab == null)
			{
				containedInTab.GetComponent<GUI_ExosuitFabricator>().OnProductAddClicked.Invoke(Product);
			}
			else
			{
				ExoFabMasterTab?.OnProductAddClicked.Invoke(Product);
			}
		}

		public void ReInit()
		{
			if (product == null)
			{
				Logger.Log("ExoFab Product not found", Category.Machines);
				return;
			}

			foreach (var element in Elements)
			{
				if (element as NetUIElement<string> != null)
				{
					(element as NetUIElement<string>).MasterSetValue(GetName(element));
				}
			}
		}

		private string GetName(NetUIElementBase element)
		{
			string nameBeforeIndex = element.name.Split('~')[0];

			if (nameBeforeIndex == "ProductName")
			{
				return Product.Name;
			}

			else if (nameBeforeIndex == "MaterialCost")
			{
				StringBuilder sb = new StringBuilder();
				string materialName;
				string materialPrice;
				sb.Append("Cost: ");

				foreach (MaterialSheet material in Product.materialToAmounts.Keys)
				{
					materialName = material.displayName;
					materialPrice = Product.materialToAmounts[material].ToString();
					sb.Append(materialPrice + " " + materialName + " " + "| ");
				}

				return sb.ToString();
			}

			return default;
		}
	}
}
