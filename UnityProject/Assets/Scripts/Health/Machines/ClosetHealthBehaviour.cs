using UnityEngine;
using UnityEngine.Networking;


	public class ClosetHealthBehaviour : NetworkBehaviour
	{
		private ClosetControl closetControl;
		private Collider2D[] colliders;
//		private PushPull objectActions;
		private RegisterCloset registerTile;

		private void Awake()
		{
			colliders = GetComponents<Collider2D>();
			registerTile = GetComponent<RegisterCloset>();
//			objectActions = GetComponent<PushPull>();
			closetControl = GetComponent<ClosetControl>();
		}

		//FIXME: this class no longer derives from LivingHealthBehaviour as it is not
		// a living thing. A new damage system is required for non living objects

		// protected override void OnDeathActions()
		// {
		// 	if (isServer)
		// 	{
		// 		ServerDeathActions();
		// 	}
		// }

		// [Server]
		// private void ServerDeathActions()
		// {
		// 	//            disableInteraction();
		// 	openCloset();
		// 	RpcClientDeathActions();
		// }

		// [ClientRpc]
		// private void RpcClientDeathActions()
		// {
		// 	disableInteraction(); //todo: refactor to use interaction bool w/ server validations
		// 	playDeathSound();
		// 	rotateSprites();
		// }

		private void disableInteraction()
		{
			for (int i = 0; i < colliders.Length; i++)
			{
				colliders[i].enabled = false;
			}

//			objectActions.BreakPull();
			registerTile.IsClosed = false;
//			objectActions.allowedToMove = false;
//			objectActions.isPushable = false;
		}

		private void playDeathSound()
		{
			Instantiate(SoundManager.Instance["Smash"], transform.position, Quaternion.identity).Play();
		}

		private void openCloset()
		{
			if (closetControl.IsClosed)
			{
				closetControl.ServerToggleCupboard();
			}
		}

		private void rotateSprites()
		{
			transform.Rotate(0, 0, 90);
		}
	}
