using System;
using System.Threading.Tasks;
using PlayGroup;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
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

		public async void AdjustOverlayPos()
		{
			await Task.Delay(TimeSpan.FromSeconds(0.1f));
			DoAdjust();
		}

		private void DoAdjust()
		{
			if (PlayerManager.LocalPlayer != null)
			{
				Vector3 playerPos =
					Camera.main.WorldToScreenPoint(PlayerManager.LocalPlayer.transform.position);
				shroud.position = playerPos;
			}
		}

		public void SetState(OverlayState state)
		{
			switch (state)
			{
				case OverlayState.normal:
					AdjustShroud(normalSettings);
					break;
				case OverlayState.injured:
					AdjustShroud(injuredSettings);
					break;
				case OverlayState.unconscious:
					AdjustShroud(unconsciousSettings);
					break;
				case OverlayState.crit:
					AdjustShroud(critcalSettings);
					break;
				case OverlayState.death:
					AdjustShroud(normalSettings);
					break;
			}
			currentState = state;
		}

		private async void AdjustShroud(ShroudPreference pref)
		{
			await Task.Delay(TimeSpan.FromSeconds(0.1f));
			if (!pref.shroudActive)
			{
				shroudImg.enabled = false;
				return;
			}
			DoAdjust();
			shroudImg.enabled = true;
			holeMat.SetFloat("_Radius", pref.holeRadius);
			holeMat.SetFloat("_Shape", pref.holeShape);
		}
	}

	[Serializable]
	public class ShroudPreference
	{
		public float holeRadius;
		public float holeShape;
		public bool shroudActive = true;
	}

	public enum OverlayState
	{
		normal,
		injured,
		unconscious,
		crit,
		death
	}
}