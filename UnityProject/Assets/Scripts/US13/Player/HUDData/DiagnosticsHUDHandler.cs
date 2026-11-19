using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Serialization;
using US13.Core.Sprite_Handler;
using US13.Objects.Engineering;
using US13.Clothing.Eyewear;
using Util;

namespace US13.Player.HUDData
{
	public class DiagnosticsHUDHandler : MonoBehaviour
	{
		[SerializeField] private SpriteHandler stateIcon = null;
		[SerializeField] private ProgressBar progressBar = null;
		private bool isVisible = false;
		public bool IsVisible => isVisible;

		[SerializeField] private RotationConstraint rotationConstraint = null;
		private static GameObject anchor = null;

		[Flags]
		public enum HUDOptions
		{
			showState = 1 << 0,
			showPower = 1 << 1,
		}

		private void Awake()
		{
			SetVisible(false, HUDOptions.showPower | HUDOptions.showState);

			if (anchor == null) anchor = new GameObject("DiagnosticsHUDRotationAnchor");

			var source = new ConstraintSource();
			source.sourceTransform = anchor.transform;
			source.weight = 1.0f;
			rotationConstraint?.AddSource(source);
		}

		public void SetVisible(bool visible, HUDOptions options)
		{
			isVisible = visible;
			if((options & HUDOptions.showState) != 0) stateIcon?.SetActive(isVisible);
			if((options & HUDOptions.showPower) != 0)
			{
				progressBar?.SetActive(isVisible);
				progressBar?.SetVisible(isVisible);
			}
		}

		public void UpdateBar(float value)
		{
			if (progressBar == null) return;
			progressBar.Value = value;
		}

		public void UpdateState(PowerState state)
		{
			if(stateIcon == null) return;
			stateIcon.SetSpriteVariant((int)state);
		}

	}
}


