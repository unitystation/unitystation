using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Matrix;

public class ItemControl : NetworkBehaviour
{

	[SyncVar(hook="UpdateState")]
	public bool aliveState = true;

	//Ignore these types
	private const string networkId = "NetworkIdentity";
	private const string networkT = "NetworkTransform";
	private const string itemControl = "ItemControl";

	public override void OnStartClient(){
		StartCoroutine(WaitForLoad());
		base.OnStartClient();
	}

	IEnumerator WaitForLoad(){
		yield return new WaitForSeconds(3f);
		UpdateState(aliveState);
	}

	void UpdateState(bool _aliveState)
	{
		MonoBehaviour[] scripts = GetComponentsInChildren<MonoBehaviour>();
		Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
		Renderer[] renderers = GetComponentsInChildren<Renderer>();
		for (int i = 0; i < scripts.Length; i++) {
			if (scripts[i].GetType().Name != networkId && scripts[i].GetType().Name != networkT
				&& scripts[i].GetType().Name != itemControl) {
				scripts[i].enabled = _aliveState;
			}
		}

		for (int i = 0; i < colliders.Length; i++) {
			colliders[i].enabled = _aliveState;
		}

		for (int i = 0; i < renderers.Length; i++) {
			renderers[i].enabled = _aliveState;
		}
			
			RegisterTile rT = GetComponent<RegisterTile>();
			if (rT != null) {
				gameObject.GetComponent<EditModeControl>().Snap();
				rT.UpdateTile(transform.position);
			}
		}
	}
