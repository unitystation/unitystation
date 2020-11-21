using System;
using UI.Core.Radial;
using UnityEngine;

namespace UI.Core.Animations
{
	[RequireComponent(typeof(IRadial))]
	public class AnimatedRadialRotation : MonoBehaviour
	{
		[SerializeField]
		private float duration = 0.1f;

		[SerializeField]
		private LeanTweenType tweenType = LeanTweenType.linear;

		private Action<float> onUpdateDelegate;

		private System.Action onCompleteDelegate;

		public System.Action OnCompleteEvent { get; set; }

		private int? TweenID { get; set; }

		private IRadial Radial { get; set; }

		public float CurrentValue
		{
			get
			{
				if (TweenID.HasValue == false)
				{
					return 0;
				}
				var descr = LeanTween.descr(TweenID.Value);
				return descr?.to.x ?? 0;
			}
		}

		private void Awake()
		{
			Radial = GetComponent<IRadial>();
			onCompleteDelegate = OnComplete;
			onUpdateDelegate = OnUpdate;
		}

		public void TweenRotation(float rotation)
		{
			if (gameObject.activeSelf == false || Mathf.Abs(rotation) < 0.01f)
			{
				return;
			}

			if (TweenID.HasValue)
			{
				var descr = LeanTween.descr(TweenID.Value);
				if (descr == null)
				{
					return;
				}
				var value = descr.to.x;
				descr.setTo(new Vector3(value + rotation, 0f, 0f));
				var durationDelta = duration * (Mathf.Abs(rotation) / 360);
				if (Mathf.Sign(rotation) == Mathf.Sign(value + rotation))
				{
					descr.setTime(descr.time + durationDelta);
				}
				else
				{
					descr.setTime(descr.time - durationDelta);
				}
			}
			else
			{
				TweenID = LeanTween.value(0, rotation, duration)
					.setOnUpdate(onUpdateDelegate)
					.setOnComplete(onCompleteDelegate)
					.setEase(tweenType)
					.id;
			}
		}

		private void OnUpdate(float value)
		{
			if (TweenID.HasValue == false)
			{
				return;
			}
			var descr = LeanTween.descr(TweenID.Value);
			descr.setTo(new Vector3(descr.to.x - value, 0f, 0f));
			Radial.RotateRadial(value);
			if (Mathf.Abs(descr.to.x) < 0.01f)
			{
				LeanTween.cancel(TweenID.Value, true);
			}
		}

		private void OnComplete()
		{
			TweenID = null;
			OnCompleteEvent?.Invoke();
		}

		private void OnDisable()
		{
			if (TweenID.HasValue)
			{
				LeanTween.cancel(TweenID.Value);
				TweenID = null;
			}
		}
	}
}
