using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UI;
using System.Collections.Generic;

namespace PlayGroup
{
	public class PlayerSprites: NetworkBehaviour
	{
		[SyncVar(hook = "FaceDirection")]
		public Vector2 currentDirection = Vector2.down;

		public PlayerMove playerMove;
		private Dictionary<string, ClothingItem> clothes = new Dictionary<string, ClothingItem>();

		void Awake()
		{
			foreach (var c in GetComponentsInChildren<ClothingItem>()) {
				clothes[c.name] = c;
			}
			FaceDirection(Vector2.down);
		}

		public void AdjustSpriteOrders(int offsetOrder){
			foreach (SpriteRenderer s in GetComponentsInChildren<SpriteRenderer>()) {
				int newOrder = s.sortingOrder;
				newOrder += offsetOrder;
				s.sortingOrder = newOrder;
			}
		}

		[Command]
		public void CmdChangeDirection(Vector2 direction){
			SetDir(direction); 
		}
		//turning character input and sprite update
		public void FaceDirection(Vector2 direction)
		{
			SetDir(direction); 
		}

		void SetDir(Vector2 direction)
		{
			if (playerMove.isGhost)
				return;
			
			if (currentDirection != direction) {
				foreach (var c in clothes.Values) {
					c.Direction = direction;
				}

				currentDirection = direction;
			}
		}
	}
}