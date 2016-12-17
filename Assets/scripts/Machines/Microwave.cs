using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI;
using Events;

public class Microwave : MonoBehaviour {

    public GameObject dishPrefab;
    	
	void OnMouseDown() {
        var item = UIManager.control.hands.CurrentSlot.Item;


        if(item) {
            var attr = item.GetComponent<ItemAttributes>();

            if(attr && attr.type == ItemType.Food) {
                UIManager.control.hands.CurrentSlot.Clear();

                var dish = Instantiate(dishPrefab);
                dish.transform.position = transform.position;

                Destroy(item);
            }
        }
    }
}
