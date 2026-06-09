using UnityEngine;
using US13.HealthV2.Living;
using US13.Managers.NetworkManagement;
using US13.Managers.UpdateManager;
using US13.Player.Movement;
using US13.Player.MovementV2;

namespace US13.UI.Core.Animations
{
	public class SillyWalk : MonoBehaviour
	{

		public float UpAndDownMagnitude = 1;
		public float RotateMagnitude = 70.88f;

		public LivingHealthMasterBase health;
		public MovementSynchronisation UOP;

		public void OnEnable()
		{
			if (CustomNetworkManager.IsHeadless) return;
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe );
		}

		public void OnDisable()
		{
			if (CustomNetworkManager.IsHeadless) return;
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe );
		}

		// Update is called once per frame
		void UpdateMe()
		{
			bool Animate = true;

			if (health.IsDead || health.IsCrit || health.IsSoftCrit)
			{
				Animate = false;
			}

			if (UOP.HasSillyWalk == false || UOP.IsCurrentlyFloating || UOP.IsFlyingSliding)
			{
				Animate = false;
			}

			if (UOP.AllowInput == false || UOP.CurrentMovementType == MovementType.Crawling)
			{
				Animate = false;
			}

			if (Animate == false)
			{
				transform.localPosition = Vector3.zero;
				transform.localRotation = Quaternion.identity;
				return;
			}

			float decimalPartx = UOP.transform.localPosition.x  - (float)Mathf.Floor(UOP.transform.localPosition.x );
			float decimalParty = UOP.transform.localPosition.y - (float)Mathf.Floor(UOP.transform.localPosition.y);

			float NonABD = 0;
			float NonABDlocalPosition = 0;
			if (decimalPartx > decimalParty)
			{
				NonABD = (UOP.transform.localPosition.x +0.25f  - (float)Mathf.Floor(UOP.transform.localPosition.x +0.25f )) - 0.5f;
				NonABDlocalPosition = (UOP.transform.localPosition.x  +0.5f - (float) Mathf.Floor(UOP.transform.localPosition.x +0.5f )) - 0.5f;
			}
			else
			{
				NonABD = (UOP.transform.localPosition.y +0.25f  - (float)Mathf.Floor(UOP.transform.localPosition.y +0.25f )) - 0.5f;
				NonABDlocalPosition = (UOP.transform.localPosition.y  +0.5f - (float) Mathf.Floor(UOP.transform.localPosition.y +0.5f)) - 0.5f;
			}

			//var NonABDlocalPosition =Mathf.Abs( Mathf.Max(decimalPartx, decimalParty) - 0.5f);

			//Loggy.Error("NonABD > " + NonABD);

			var biggest = Mathf.Abs(NonABD);
			var biggestlocalPosition = Mathf.Abs(NonABDlocalPosition);


			//Loggy.Error("biggest > " + biggest);

			var rotationy = (biggest -0.25f) * 2;
			//Loggy.Error("rotationy > " + rotationy);


			transform.localPosition =   new Vector3(0,biggestlocalPosition,0) * UpAndDownMagnitude;
			transform.localRotation  =   Quaternion.Euler(new Vector3(0,0,rotationy)* RotateMagnitude) ;
		}
	}
}
