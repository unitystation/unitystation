using UI;
using UnityEngine;
using UnityEngine.UI;

public class StockItem: MonoBehaviour
{
    // Public references for the name of the stock item, its current stock,
    // the vendor trigger component, and the item we're going to vend
    public Text stockName;
    public Text StockAmts;
    public VendorTrigger vendorTrigger;
    public GameObject vendItem;

    public void UpdateStockItem()
    {
        stockName.text = vendItem.name;
        StockAmts.text = vendorTrigger.stockAmts[vendItem].ToString();
    }
}