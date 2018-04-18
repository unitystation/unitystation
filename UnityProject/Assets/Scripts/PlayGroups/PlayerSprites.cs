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
		[SyncVar(hook = nameof( FaceDirectionSync ))] public Orientation currentDirection;
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
			FaceDirection(Orientation.Down);
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
		public void CmdChangeDirection(Orientation direction)
		{
			FaceDirection(direction);
		}

		//turning character input and sprite update for local only! (prediction)
		public void FaceDirection( Orientation direction )
		{
			SetDir(direction);
		}

		//For syncing all other players (not locally owned)
		private void FaceDirectionSync(Orientation dir)
		{
			if (PlayerManager.LocalPlayer != gameObject) {
				currentDirection = dir;
				SetDir(dir);
			}
		}


		public void SetDir(Orientation direction)
		{
			if (playerMove.isGhost) {
				return;
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
			if ( Math.Abs( rot ) > 0 ) {
				//So other players can walk over the Unconscious
				AdjustSpriteOrders(-30);
			}
		}
		/// Not letting transform.rotation deviate from intended value
		/// (For keeping players upright on rotating matrices)
		private void RefreshRotation() {
			transform.rotation = Rotation;
		}
		/// Changes direction by degrees; positive = CW, negative = CCW
		public void ChangePlayerDirection( int degrees ) {
			for ( int i = 0; i < Math.Abs(degrees/90); i++ ) {
				if ( degrees < 0 ) {
					ChangePlayerDirection(currentDirection.Previous());
				} else {
					ChangePlayerDirection(currentDirection.Next());
				}
			}
		}

		public void ChangePlayerDirection( Orientation orientation ) {
			CmdChangeDirection(orientation);
			//Prediction
			FaceDirection(orientation);
		}
	}
}