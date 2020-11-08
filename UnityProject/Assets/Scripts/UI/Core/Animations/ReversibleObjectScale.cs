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

	private int TweenID { get; set; }

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

		if (TweenID != 0)
		{
			var descr = LeanTween.descr(TweenID);
			if (descr != null && LeanTween.isTweening(TweenID))
			{
				timePassed = descr.passed;
			}
			LeanTween.cancel(TweenID);
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
		if (TweenID != 0)
		{
			LeanTween.cancel(TweenID);
		}

		transform.localScale = scaleStart;
		Direction = AnimationDirection.None;
	}
}
