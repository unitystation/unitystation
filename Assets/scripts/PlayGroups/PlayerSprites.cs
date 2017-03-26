using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UI;
using System.Collections.Generic;

namespace PlayGroup
{
	public class PlayerSprites: NetworkBehaviour
	{
		[HideInInspector]

		[SyncVar(hook = "FaceDirection")]
		public Vector2 currentDirection = Vector2.down;
		private PlayerScript playerScript;

		private Dictionary<string, ClothingItem> clothes = new Dictionary<string, ClothingItem>();

		void Awake()
		{
			foreach (var c in GetComponentsInChildren<ClothingItem>()) {
				clothes[c.name] = c;
			}
			FaceDirection(Vector2.down);
			playerScript = gameObject.GetComponent<PlayerScript>();
		}

		//turning character input and sprite update
		public void FaceDirection(Vector2 direction)
		{
			if (playerScript != null) {
				if (isLocalPlayer) {//if this player is mine, then update your dir on all other client
					SetDir(direction);
				} else {
					SetDir(direction); 
				}
			} else {
				SetDir(direction); //dev mode
			}
		}

		void SetDir(Vector2 direction)
		{
			if (currentDirection != direction) {
				foreach (var c in clothes.Values) {
					c.Direction = direction;
				}

				currentDirection = direction;
			}
		}

		public void PickedUpItem(GameObject item)
		{
			if (UIManager.Hands.IsRight) {
				clothes["rightHand"].UpdateItem(item);
			} else {
				clothes["leftHand"].UpdateItem(item);
			}
		}

		public void RemoveItemFromHand(bool rightHand)
		{
			if (rightHand) {
				clothes["rightHand"].Clear();
			} else {
				clothes["leftHand"].Clear();
			}
		}
	}
}