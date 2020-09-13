using System.Collections;
using NaughtyAttributes;
using UnityEngine;

namespace UI.Animations
{
	public class SimpleFadeInFadeOut: AnimationBase
	{
		[InfoBox("When this game object is active in hierarchy, it will play a little fade in and fade out animation." +
		         "The totalTime variable will define how long it will take in seconds to do the entire animation.")]
		[SerializeField][Tooltip("How much time should the animation take." +
		                         " From the total time 25% will be the in and out transition, 75% will be static")]
		private float totalTime = default;

		private float TransitionTime => totalTime * 0.125f;
		private float StaticTime => totalTime * 0.75f;

		private void FadeIn()
		{
			LeanTween.alpha(gameObject, 0f, TransitionTime).setEase(LeanTweenType.linear);
		}

		private void FadeOut()
		{
			LeanTween.alpha(gameObject, 1f, TransitionTime).setEase(LeanTweenType.linear);
		}

		public override void OnEnable()
		{
			StartCoroutine(PlayAnimation());
		}

		IEnumerator PlayAnimation()
		{
			FadeIn();
			yield return WaitFor.Seconds(TransitionTime);
			yield return WaitFor.Seconds(StaticTime);
			FadeOut();
			yield return WaitFor.Seconds(TransitionTime);
			gameObject.SetActive(false);
		}

	}
}