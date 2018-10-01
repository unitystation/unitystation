using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RadialMenuSpawner : MonoBehaviour {

	public static RadialMenuSpawner ins;
	public RadialMenu menuPrefab;

	void Awake(){
		if(ins == null){
			ins = this;
		} else {
			Destroy(this);
		}
	}
	public void SpawnRadialMenu(List<Rightclick.Menu> ListRightclick){
		RadialMenu newMenu = Instantiate (menuPrefab) as RadialMenu;
		newMenu.transform.SetParent (transform, false);
		newMenu.transform.position = Input.mousePosition;
		newMenu.SetupMenu(ListRightclick); 
	}

}

