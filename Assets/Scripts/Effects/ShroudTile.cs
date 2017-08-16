using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
public class ShroudTile : MonoBehaviour {

	private bool shouldCheck = false;
	public Renderer renderer;
	void OnEnable(){
		renderer.enabled = true;
		shouldCheck = true;
	}

	void Update(){
		if (shouldCheck) {
			if (!renderer.isVisible) {
				shouldCheck = false;
				ReturnToPool();
			}
		}
	}
		
	private void ReturnToPool(){
		Profiler.BeginSample("Shroud Pool Return");
		DisplayManager.Instance.fieldOfView.shroudTiles.Remove(transform.position);
		PoolManager.PoolClientDestroy(gameObject);
		Profiler.EndSample();
	}
}
