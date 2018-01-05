using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace PlayGroup
{
	public class PlayerSprites : NetworkBehaviour
	{
		private readonly Dictionary<string, ClothingItem> clothes = new Dictionary<string, ClothingItem>();
		[SyncVar(hook = "FaceDirectionSync")] public Vector2 currentDirection;

		public PlayerMove playerMove;

		private void Awake()
		{
			foreach (ClothingItem c in GetComponentsInChildren<ClothingItem>()) {
				clothes[c.name] = c;
			}
		}

		public override void OnStartServer()
		{
			FaceDirection(Vector2.down);
			base.OnStartServer();
		}

		public override void OnStartClient()
		{
			StartCoroutine(WaitForLoad());
			base.OnStartClient();
		}

		private IEnumerator WaitForLoad()
		{
			yield return new WaitForSeconds(2f);
			FaceDirectionSync(currentDirection);
		}

		public void AdjustSpriteOrders(int offsetOrder)
		{
			foreach (SpriteRenderer s in GetComponentsInChildren<SpriteRenderer>()) {
				int newOrder = s.sortingOrder;
				newOrder += offsetOrder;
				s.sortingOrder = newOrder;
			}
		}

		[Command]
		public void CmdChangeDirection(Vector2 direction)
		{
			FaceDirection(direction);
		}

		//turning character input and sprite update for local only! (prediction)
		public void FaceDirection(Vector2 direction)
		{
			SetDir(direction);
		}

		//For syncing all other players (not locally owned)
		private void FaceDirectionSync(Vector2 dir)
		{
			if (PlayerManager.LocalPlayer != gameObject) {
				currentDirection = dir;
				SetDir(dir);
			}
		}


		public void SetDir(Vector2 direction)
		{
			if (playerMove.isGhost) {
				return;
			}
			if (direction.x != 0f && direction.y != 0f) {
				direction.y = 0f;
			}

			foreach (ClothingItem c in clothes.Values) {
				c.Direction = direction;
			}

			currentDirection = direction;
		}

	}
}