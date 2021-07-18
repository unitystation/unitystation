using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;


	/// <summary>
	///     To control the critical overlays (unconscious, dying, oxygen loss etc)
	/// </summary>
	public class OverlayCrits : MonoBehaviour
	{
		public OverlayState currentState;
		public Material holeMat;
		public RectTransform shroud;
		public Image shroudImg;

		private bool MonitorTarget = false;
		private Vector3 positionCache = Vector3.zero;

		public Color shroudColor;
		public float Radius = 5;

		public float Epow = 2;

		void LateUpdate()
		{
			if (MonitorTarget)
			{
				if (PlayerManager.LocalPlayer != null)
				{
					Vector3 playerPos =
						Camera.main.WorldToScreenPoint(PlayerManager.LocalPlayer.transform.position);
					shroud.position = playerPos;
				}
			}
		}

		 public void SetState(OverlayState state)
		 {
			 switch (state)
			 {
			 	case OverlayState.normal:
				    SetState(1f);
			 		break;
			 	case OverlayState.injured:
				    SetState(0.50f);
			 		break;
			 	case OverlayState.unconscious:
				    SetState(-0.25f);
			 		break;
			 	case OverlayState.crit:
				    SetState(-0.75f);
			 		break;
			 	case OverlayState.death:
				    SetState(-1.1f);
			 		break;
			 }
			currentState = state;
		 }



		public void SetState(float state)
		{
			AdjustShroud( state);

		}

		void AdjustShroud(float state)
		{
			if (PlayerManager.LocalPlayerScript.OrNull()?.mind != null)
			{
				if (PlayerManager.LocalPlayerScript.mind.IsGhosting)
				{
					state = 1;
				}
			}

			if (state <= -1)
			{
				//is Dead do not show overly
				state = 1;
			}


			//_Color A 0.0 to 0.60
			//0.0 = 0.5 to 0.60 = -0.66

			if (state < 0.5f)
			{
				var PercentagePower = Mathf.Abs(((state+0.66f)/(1.16f)));
				PercentagePower = Mathf.Clamp(PercentagePower, 0, 1);
				// )
				shroudColor.a = Mathf.Lerp(0.60f , 0.0f, Mathf.Pow(PercentagePower, (float)Math.E * Epow));
			}
			else
			{
				shroudColor.a = 0;
			}

			//_Radius, 1 to 0
			// 1 = 0.5 to  0  = -0.66

			if (state < 0.5f)
			{
				var PercentagePower = Mathf.Clamp(((state + 0.66f) / (1.16f)), 0f, 1f);
				Radius = Mathf.Lerp(0f, 1f, Mathf.Pow(PercentagePower,(float)Math.E * Epow));
			}
			else
			{
				Radius = 5;
			}


			holeMat.SetColor("_Color", shroudColor);
			holeMat.SetFloat("_Radius", Radius);
			holeMat.SetFloat("_Shape", 1f);

			shroudImg.enabled = true;
		}


	}

	[Serializable]
	public class ShroudPreference
	{
		public float holeRadius;
		public float holeShape;
		public bool shroudActive = true;

		public Color shroudColor;
	}

	public enum OverlayState
	{
		normal,
		injured,
		unconscious,
		crit,
		death
	}
