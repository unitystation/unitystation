using UnityEngine;
using System.Collections;
using PlayGroup;

namespace Items{
public class ItemTriggers : MonoBehaviour {

    public bool allowedPickUp = true;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}


		void OnMouseDown(){
			
			//and eventually check if there is room in the hand etc
			if (Managers.control.playerScript != null) {
				var headingToPlayer = Managers.control.playerScript.transform.position - transform.position;
				var distance = headingToPlayer.magnitude;
				var direction = headingToPlayer / distance;
				Debug.Log ("DISTANCE: " + distance + " DIRECTION: " + direction);

				if (distance <= 2f && allowedPickUp) {
					//add to hand
					ItemManager.control.PickUpObject (this.gameObject);


				}
			}

		}
}
}
