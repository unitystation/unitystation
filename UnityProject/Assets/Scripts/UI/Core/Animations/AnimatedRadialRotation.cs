using System;
using UI.Core.Radial;
using UnityEngine;

[RequireComponent(typeof(IRadial))]
public class AnimatedRadialRotation : MonoBehaviour
{
	[SerializeField]
	private float duration;

	[SerializeField]
	private LeanTweenType tweenType;

	private Action onCompleteDelegate;

	private Action<float> onUpdateDelegate;

	public Action OnCompleteEvent { get; set; }

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
		if (gameObject.activeSelf == false || rotation > -0.01f && rotation < 0.01f)
		{
			return;
		}

		if (TweenID.HasValue)
		{
			var descr = LeanTween.descr(TweenID.Value);
			if (descr != null)
			{
				var value = descr.to.x;
				descr.setTo(new Vector3(value + rotation, 0f, 0f));
				if (Mathf.Sign(rotation) != Mathf.Sign(value))
				{
					descr.setTime(duration);
					descr.setPassed(0);
				}
				else
				{
					descr.setTime(descr.time + Mathf.Min(duration, duration / (value / rotation)));
				}
			}
		}
		else
		{
			var descr = LeanTween.value(gameObject, 0, rotation, duration);
			if (descr == null)
			{
				Logger.LogError($"Unable to create LeanTween description for {nameof(AnimatedRadialRotation)}.");
				return;
			}
			TweenID = descr
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
			LeanTween.cancel(TweenID.Value, true);
			TweenID = null;
		}
	}
}
