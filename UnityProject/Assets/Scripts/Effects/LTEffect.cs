using Mirror;
using UnityEngine;
using Util;

namespace Effects
{
	public class LTEffect : NetworkBehaviour
	{
		/// <summary>
		/// The base class for all animated effects that requires LeanTween to sync between all clients.
		/// </summary>

		protected Vector3 originalPosition;

		[Tooltip("Which Axis will the animation play on?")]
		[SerializeField]
		protected NetworkedLeanTween.Axis axisMode = NetworkedLeanTween.Axis.X;

		[SerializeField, Tooltip("Do you want to animate the entire gameObject or just the sprite?")]
		protected AnimMode animType = AnimMode.SPRITE;

		[Tooltip("The sprite gameObject that will be used for the animation.")]
		[SerializeField]
		protected Transform spriteReference;

		[Tooltip("The NetworkedLeanTween compontent that let's [ClientRpc] LeanTween functions to be called.")]
		public NetworkedLeanTween tween;

		protected enum AnimMode
		{
			SPRITE,
			GAMEOBJECT
		}


		private void Awake()
		{
			GetOriginalPosition();
		}

		public virtual void StopAnimation()
		{
			tween.RpcCancelObject(this.gameObject, false);
		}

		[ClientRpc]
		public virtual void RpcStopAnim()
		{
			tween.RpcCancelObject(this.gameObject, false);
		}

		public void GetOriginalPosition()
		{
			originalPosition = this.gameObject.RegisterTile().WorldPositionServer;
		}
	}
}