using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagazineBehaviour : MonoBehaviour 
{
	public int magazineSize=20;
	public int ammoRemains;
	public bool Usable;

	void Start () {
		Usable = true;
		ammoRemains = magazineSize;
	}
	

	void Update () {
		if (ammoRemains <= 0) {
			Usable = false;
		}
	}
}
