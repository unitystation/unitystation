using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorTile : MonoBehaviour {

	public GameObject fireScorch;

	public void AddFireScorch(){
		if (fireScorch == null) {
		//Do poolspawn here
			fireScorch = ItemFactory.Instance.SpawnScorchMarks(transform);
		} 
	}

	public void CleanTile(){
		if (fireScorch != null) {
			fireScorch.transform.parent = null;
			PoolManager.PoolClientDestroy(fireScorch);
		}
	}
}
