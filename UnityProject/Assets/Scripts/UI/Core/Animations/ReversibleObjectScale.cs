using UnityEngine;

public class ReversibleObjectScale : MonoBehaviour
{
	[SerializeField]
	private float duration;

	[SerializeField]
	private Vector3 scaleStart;

	[SerializeField]
	private Vector3 scaleEnd;

	[SerializeField]
	private LeanTweenType tweenType;

	private AnimationDirection Direction { get; set; } = AnimationDirection.None;

	private int? TweenID { get; set; }

	public void TweenScale(bool forward)
	{
		TweenScale(forward ? AnimationDirection.Forward : AnimationDirection.Backward);
	}

	public void TweenScale(AnimationDirection direction)
	{
		if (gameObject.activeSelf == false || Direction == direction)
		{
			return;
		}

		float timePassed;
		float tweenDirection;
		if (direction == AnimationDirection.Forward)
		{
			timePassed = 0f;
			tweenDirection = 1f;
		}
		else
		{
			timePassed = Direction == AnimationDirection.None ? 0f : 1f;
			tweenDirection = -1f;
		}

		if (TweenID.HasValue)
		{
			var descr = LeanTween.descr(TweenID.Value);
			if (descr != null && LeanTween.isTweening(TweenID.Value))
			{
				timePassed = descr.passed;
			}
			LeanTween.cancel(TweenID.Value);
		}

		Direction = direction;
		TweenID = LeanTween.scale(gameObject, scaleEnd, duration)
			.setFrom(scaleStart)
			.setDirection(tweenDirection)
			.setPassed(timePassed)
			.setEase(tweenType)
			.id;
	}

	private void OnDisable()
	{
		if (TweenID.HasValue)
		{
			LeanTween.cancel(TweenID.Value);
			TweenID = null;
		}

		transform.localScale = scaleStart;
		Direction = AnimationDirection.None;
	}
}
