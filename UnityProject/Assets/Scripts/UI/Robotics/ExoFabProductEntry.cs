using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class ExoFabProductEntry : MonoBehaviour
{
	public NetLabel productName;
	public NetLabel productPrice;

	public ExoFabProductButton addToQueueButton;
	public ExoFabProductButton inspectionButton;
	private StringBuilder sb = new StringBuilder();

	public void Setup(MachineProduct product, Dictionary<ItemTrait, string> materialsToName)
	{
		//Need to change this game object's name to something unique due to Network IDs
		this.gameObject.name = product.name + "ProductEntry";

		productName.SetValue = product.name;
		productName.gameObject.name = product.name + "ProductEntryLabel";

		//Sets the product price text
		sb.Append("Cost: ");
		foreach (MachineProductMaterialPrice materialPrice in product.materialPrice)
		{
			sb.Append(materialPrice.amount + " " + materialsToName[materialPrice.material] + " | ");
		}
		productPrice.SetValue = sb.ToString();
		sb.Clear();
		productPrice.gameObject.name = product.name + "PriceLabel";

		//gives buttons the product they send when clicked on.
		addToQueueButton.gameObject.name = product.name + "AddToQueueButton";
		addToQueueButton.machineProduct = product;

		inspectionButton.gameObject.name = product.name + "InspectionButton";
		inspectionButton.machineProduct = product;
	}
}