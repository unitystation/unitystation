using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


	public class PlayerSprites : NetworkBehaviour
	{
		private readonly Dictionary<string, ClothingItem> clothes = new Dictionary<string, ClothingItem>();
		[SyncVar(hook = nameof( FaceDirectionSync ))]
		public Orientation currentDirection;

		public PlayerMove playerMove;

		private void Awake()
		{
			foreach (ClothingItem c in GetComponentsInChildren<ClothingItem>()) {
				clothes[c.name] = c;
			}
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
			//		Logger.LogWarning("Setting TileType to none for player and adjusting sortlayers in RpcSetPlayerRot");
			SpriteRenderer[] spriteRends = GetComponentsInChildren<SpriteRenderer>();
			foreach (SpriteRenderer sR in spriteRends)
			{
				sR.sortingLayerName = "Blood";
			}
			gameObject.GetComponent<RegisterPlayer>().IsBlocking = false;
			gameObject.GetComponent<ForceRotation>().Rotation.eulerAngles = new Vector3(0,0,rot);

			//Might be no longer needed as all spriteRenderers are set to Blood layer
			// if ( Math.Abs( rot ) > 0 ) {
			// 	//So other players can walk over the Unconscious
			// 	AdjustSpriteOrders(-30);
			// }
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
