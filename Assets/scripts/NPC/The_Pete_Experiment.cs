using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class The_Pete_Experiment : NetworkBehaviour {

	public GameObject petePrefab;
	public override void OnStartServer(){
		for (float i = 0 ; i < 6; i++) {
			GameObject pete = GameObject.Instantiate(petePrefab, new Vector3(1842f + i, 1149f, 0f), Quaternion.identity);
			NetworkServer.Spawn(pete);
		}
		base.OnStartServer();
	}
}
