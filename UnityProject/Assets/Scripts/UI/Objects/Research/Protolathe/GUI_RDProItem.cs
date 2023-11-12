using System.Collections.Generic;
using System.Text;
using Logs;
using UnityEngine;
using UI.Core.NetUI;
using Systems.Research;

namespace UI.Objects
{
	public class GUI_RDProItem : DynamicEntry
	{
		private GUI_RDProductionMachine RDProMasterTab = null;
		private string product = null;
		private Design design = null;

		public string Product {
			get => product;
			set {
				product = value;
				ReInit();
			}
		}

		public void AddToQueue()
		{
			if (RDProMasterTab == null)
			{
				containedInTab.GetComponent<GUI_RDProductionMachine>().OnProductAddClicked.Invoke(Product);
			}
			else
			{
				RDProMasterTab?.OnProductAddClicked.Invoke(Product);
			}
		}

		public void ReInit()
		{
			if (product == null)
			{
				Loggy.Log("Machine Product not found", Category.Machines);
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
			design = Designs.Globals.InternalIDSearch[product];

			string nameBeforeIndex = element.name.Split('~')[0];

			if (nameBeforeIndex == "ProductName")
			{
				return design.Name;
			}
			else if (nameBeforeIndex == "MaterialCost")
			{
				StringBuilder sb = new StringBuilder();
				string materialName;
				string materialPrice;
				sb.Append("Cost: ");
				foreach(KeyValuePair<string,int> material in design.Materials)
				{
					materialName = material.Key;
					materialPrice = material.Value.ToString();
					sb.Append(materialPrice + " " + materialName + " " + "| ");
				}

				return sb.ToString();
			}

			return default;
		}
	}
}
