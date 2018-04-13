using System;
using System.Collections;
using System.Collections.Generic;
using Tilemaps.Behaviours.Objects;
using UnityEngine;
using UnityEngine.Networking;

namespace PlayGroup
{
	public class PlayerSprites : ManagedNetworkBehaviour
	{
		private readonly Dictionary<string, ClothingItem> clothes = new Dictionary<string, ClothingItem>();
		[SyncVar(hook = "FaceDirectionSync")] public Vector2 currentDirection;
		public Quaternion Rotation;
		public PlayerMove playerMove;

		private void Awake()
		{
			foreach (ClothingItem c in GetComponentsInChildren<ClothingItem>()) {
				clothes[c.name] = c;
			}
			Rotation = Quaternion.Euler( Vector3.zero );
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

		public override void UpdateMe() {
			if ( transform.rotation != Rotation ) {
				RefreshRotation();
			}
		}

		[Command]
		public void CmdChangeDirection(Vector2 direction)
		{
			FaceDirection(direction);
		}

		public void TurnDirection( bool clockwise ) {
			currentDirection = Vector2Int.RoundToInt(Quaternion.Euler( 0, 0, clockwise ? -90 : 90 ) * currentDirection);
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
		///For falling over and getting back up again over network
		[ClientRpc]
		public void RpcSetPlayerRot( float rot )
		{
			//		Debug.LogWarning("Setting TileType to none for player and adjusting sortlayers in RpcSetPlayerRot");
			SpriteRenderer[] spriteRends = GetComponentsInChildren<SpriteRenderer>();
			foreach (SpriteRenderer sR in spriteRends)
			{
				sR.sortingLayerName = "Blood";
			}
			gameObject.GetComponent<RegisterPlayer>().IsBlocking = false;
			Rotation.eulerAngles = new Vector3(0,0,rot);
//			RefreshRotation();
			if ( Math.Abs( rot ) > 0 ) {
				//So other players can walk over the Unconscious
				AdjustSpriteOrders(-30);
			}
		}

		private void RefreshRotation() {
			transform.rotation = Rotation;
		}

		public void ChangePlayerDirectionRelative( float relAngle ) {
			ChangePlayerDirection( Vector2.Angle(Vector2.zero, currentDirection) + relAngle );
		}

		public void ChangePlayerDirection( float absAngle )
		{
			if (absAngle >= 315f && absAngle <= 360f || absAngle >= 0f && absAngle <= 45f)
			{
				CmdChangeDirection(Vector2.up);
				//Prediction
				FaceDirection(Vector2.up);
			}
			if (absAngle > 45f && absAngle <= 135f)
			{
				CmdChangeDirection(Vector2.right);
				//Prediction
				FaceDirection(Vector2.right);
			}
			if (absAngle > 135f && absAngle <= 225f)
			{
				CmdChangeDirection(Vector2.down);
				//Prediction
				FaceDirection(Vector2.down);
			}
			if (absAngle > 225f && absAngle < 315f)
			{
				CmdChangeDirection(Vector2.left);
				//Prediction
				FaceDirection(Vector2.left);
			}
		}
	}
}