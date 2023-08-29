using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;


	/// <summary>
	///     To control the critical overlays (unconscious, dying, oxygen loss etc)
	/// </summary>
	public class OverlayCrits : MonoBehaviour
	{
		public static OverlayCrits Instance;

		public Material holeMat;

		private bool MonitorTarget = false;

		public Color TargetshroudColor = Color.red;
		public Color shroudColor = Color.red;
		public float Radius = 5;

		public float Epow = 2;

		public float TargetRadius = 3;

		public void Awake()
		{
			Instance = this;
		}

		void LateUpdate()
		{
			if (MonitorTarget)
			{
				if (PlayerManager.LocalPlayerObject != null)
				{
					Vector3 playerPos = Camera.main.WorldToScreenPoint(PlayerManager.LocalPlayerObject.transform.position);
				}
			}
		}

		private void Update()
		{
			shroudColor = Color.Lerp(shroudColor, TargetshroudColor, Time.deltaTime);
			Radius = Mathf.Lerp(Radius, TargetRadius, Time.deltaTime);
		}


		void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			holeMat.SetColor("_Color", shroudColor);
			holeMat.SetFloat("_Radius", Radius);
			holeMat.SetFloat("_Shape", 1f);

			Graphics.Blit(source, destination, holeMat);
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
		 }



		public void SetState(float state)
		{
			AdjustShroud( state);

		}

		void AdjustShroud(float state)
		{
			if (PlayerManager.LocalPlayerScript.OrNull()?.Mind != null)
			{
				if (PlayerManager.LocalMindScript.IsGhosting)
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
				TargetshroudColor.a = Mathf.Lerp(0.60f , 0.0f, Mathf.Pow(PercentagePower, (float)Math.E * Epow));
			}
			else
			{
				TargetshroudColor.a = 0;
			}

			//_Radius, 1 to 0
			// 1 = 0.5 to  0  = -0.66

			if (state < 0.5f)
			{
				var PercentagePower = Mathf.Clamp(((state + 0.66f) / (1.16f)), 0f, 1f);
				TargetRadius = Mathf.Lerp(0f, 1f, Mathf.Pow(PercentagePower,(float)Math.E * Epow));
			}
			else
			{
				TargetRadius = 3;
			}
		}


	}

	public enum OverlayState
	{
		normal,
		injured,
		unconscious,
		crit,
		death
	}
