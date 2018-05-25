using UI;
using UnityEngine;
using UnityEngine.UI;

public class GUI_Vending : MonoBehaviour
{
    public GUI_Vending vendingGUI;
    private VendorTrigger currentVendingTrigger;

    // References to our stock item prefab and our content in our scroll view
    public GameObject stockItemPrefab;
    public GameObject content;

    public void BtnOk()
	{
		SoundManager.Play("Click01");
		gameObject.SetActive(false);
	}

	public void EndEditOnEnter()
	{
		if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
		{
			BtnOk();
		}
	}

    // Let's cache our vendorTrigger component
    public void OpenWindow(VendorTrigger vendorTrigger)
    {
        currentVendingTrigger = vendorTrigger;
        PopulateContents();
    }

    // Let's ask the vendorTrigger to vend an item
    public void VendItem(GameObject item)
    {
        currentVendingTrigger.Vend(item);
    }

    // This lets us get all of the children of our gameobject
    public void OnEnable()
    {
        foreach(Transform t in content.transform)
        {
            Destroy(t.gameObject);
        }
    }

    public void PopulateContents()
    {
        foreach (GameObject item in currentVendingTrigger.vendorcontent)
        {
            // Instantiate a new "stock item" object so that we can put it in the list
            // Parent and transform it so it looks nice and pretty
            GameObject vendingItem = Instantiate(stockItemPrefab);
            vendingItem.transform.SetParent(content.transform);
            vendingItem.transform.localScale = Vector3.one;
            vendingItem.transform.localPosition = Vector3.zero;

            // Let's get the StockItem component from our items and cache the vendor trigger component
            StockItem stockItem = vendingItem.GetComponent<StockItem>();
            stockItem.vendorTrigger = currentVendingTrigger;

            // Make the item we're going to vend our current item and update its values
            stockItem.vendItem = item;
            stockItem.UpdateStockItem();
        }
    }
}