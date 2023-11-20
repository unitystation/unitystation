using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EtherealThing : MonoBehaviour, IServerSpawn
{
	public Vector3 SavedLocalPosition;

	public void OnSpawnServer(SpawnInfo info)
	{

		StartCoroutine(WaitingFrame());
	}
	private IEnumerator WaitingFrame()
	{
		yield return null;
		var RegisterTile = this.GetComponent<RegisterTile>();
		var localPosition = this.transform.localPosition;
		SavedLocalPosition = localPosition;
		RegisterTile.Matrix.MetaDataLayer.EtherealThings.Add(this);
		this.GetComponent<UniversalObjectPhysics>()?.DisappearFromWorld();
	}



}
