using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using PlayGroup;

namespace Tilemaps.Behaviours.Objects
{
	[ExecuteInEditMode]
	public class RegisterPlayer : RegisterTile
	{
		public bool IsBlocking { get; set; } = true;
		private bool isChangingMatricies = false;
		private Transform changingCheck;


		public override bool IsPassable()
		{
			return !IsBlocking;
		}

		void OnTriggerEnter2D(Collider2D coll)
		{
			//layer 24 is matrix layer
			if (coll.gameObject.layer == 24 && isServer) {
				Debug.Log("PlayerEntered Matrix: " + coll.gameObject.transform.parent.name);
				if (!isChangingMatricies) {
					ChangeMatricies(coll.gameObject.transform.parent);
					isChangingMatricies = true;
					StartCoroutine(ChangeCoolDown(coll.gameObject.transform.parent));
				} else {
					changingCheck = coll.gameObject.transform.parent;
				}
			}
		}

		[Server]
		private void ChangeMatricies(Transform newParent){
			NetworkIdentity netIdent = newParent.GetComponent<NetworkIdentity>();
			if (ParentNetId != netIdent.netId) {
				ParentNetId = netIdent.netId;
			}

			PlayerSync playerSync = GetComponent<PlayerSync>();
			if (playerSync != null) {
				playerSync.SetPosition(transform.localPosition);
			}
		}

		IEnumerator ChangeCoolDown(Transform newParent){
			//Wait 10ms and check which matrix the player did end up on
			//Or else player will get stuck between two matricies
			yield return new WaitForSeconds(0.1f);
			if(changingCheck != null){
				if(changingCheck != newParent){
					ChangeMatricies(changingCheck);
				}
			}
			isChangingMatricies = false;
		}
	}
}