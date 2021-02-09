using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;


	/// <summary>
	///     To control the critical overlays (unconscious, dying, oxygen loss etc)
	/// </summary>
	public class OverlayCrits : MonoBehaviour
	{
		public ShroudPreference critcalSettings;

		public OverlayState currentState;
		public Material holeMat;
		public ShroudPreference injuredSettings;

		public ShroudPreference normalSettings;
		public RectTransform shroud;
		public Image shroudImg;
		public ShroudPreference unconsciousSettings;

		private bool MonitorTarget = false;
		private Vector3 positionCache = Vector3.zero;

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
					StartCoroutine(AdjustShroud(normalSettings));
					break;
				case OverlayState.injured:
					StartCoroutine(AdjustShroud(injuredSettings));
					break;
				case OverlayState.unconscious:
					StartCoroutine(AdjustShroud(unconsciousSettings));
					break;
				case OverlayState.crit:
					StartCoroutine(AdjustShroud(critcalSettings));
					break;
				case OverlayState.death:
					StartCoroutine(AdjustShroud(normalSettings));
					break;
			}
			currentState = state;
		}

		IEnumerator AdjustShroud(ShroudPreference pref)
		{
			yield return WaitFor.Seconds(.1f);
			if (!pref.shroudActive)
			{

				shroudImg.enabled = false;
				MonitorTarget = false;
				yield break;
			}

			MonitorTarget = true;
			holeMat.SetColor("_Color", pref.shroudColor);
			holeMat.SetFloat("_Radius", pref.holeRadius);
			holeMat.SetFloat("_Shape", pref.holeShape);

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
