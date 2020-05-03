using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class GUI_AutolatheItem : DynamicEntry
{
	private GUI_Autolathe AutolatheMasterTab = null;
	private MachineProduct product = null;

	public MachineProduct Product
	{
		get
		{
			return product;
		}
		set
		{
			product = value;
			ReInit();
		}
	}

	public void AddToQueue()
	{
		if (AutolatheMasterTab == null) { MasterTab.GetComponent<GUI_Autolathe>().OnProductAddClicked.Invoke(Product); }
		else { AutolatheMasterTab?.OnProductAddClicked.Invoke(Product); }
	}

	public void ReInit()
	{
		if (product == null)
		{
			Logger.Log("ExoFab Product not found");
			return;
		}
		foreach (var element in Elements)
		{
			string nameBeforeIndex = element.name.Split('~')[0];
			switch (nameBeforeIndex)
			{
				case "ProductName":
					((NetUIElement<string>)element).SetValueServer(Product.Name);
					break;

				case "MaterialCost":
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
					((NetUIElement<string>)element).SetValueServer(sb.ToString());
					break;
			}
		}
	}
}