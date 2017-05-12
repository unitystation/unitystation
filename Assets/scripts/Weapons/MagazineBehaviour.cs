using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class MagazineBehaviour : NetworkBehaviour 
{
	public string ammoType; //SET IT IN INSPECTOR
	public int magazineSize=20;
	public bool Usable;

	[SyncVar]
	public int ammoRemains;

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
