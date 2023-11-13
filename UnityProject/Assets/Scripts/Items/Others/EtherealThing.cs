using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EtherealThing : MonoBehaviour, IServerSpawn
{
	public void OnSpawnServer(SpawnInfo info)
	{

		StartCoroutine(WaitingFrame());
	}
	private IEnumerator WaitingFrame()
	{
		yield return null;
		var RegisterTile = this.GetComponent<RegisterTile>();
		RegisterTile.Matrix.MetaDataLayer.InitialObjects[this.gameObject] = this.transform.localPosition;
		this.GetComponent<UniversalObjectPhysics>()?.DisappearFromWorld();
		this.GetComponent<RegisterTile>().UpdatePositionServer();
	}



}
