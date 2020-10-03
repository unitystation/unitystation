using System.Collections;
using NaughtyAttributes;
using UnityEngine;

namespace UI.Animations.Presets
{
	[RequireComponent(typeof(CanvasGroup))]
	public class SimpleFadeInFadeOut: AnimationBase
	{
		[InfoBox("The animation will start as soon as the object is active in hierarchy!", EInfoBoxType.Warning)]
		[SerializeField][Tooltip("Add a canvas group to the object root and set it here so everything is animated.")]
		private CanvasGroup canvasGroup = default;

		[SerializeField][Tooltip("Time in seconds the animation will transition.")]
		private float transitionTime = 1.25f;
		[SerializeField][Tooltip("Time in seconds the animation will stay.")]
		private float stayTime	 = 5f;

		private void Awake()
		{
			canvasGroup = GetComponent<CanvasGroup>();
			canvasGroup.alpha = 0f;
		}

		private void FadeIn()
		{
			LeanTween.alphaCanvas(canvasGroup, 1f, transitionTime).setEase(LeanTweenType.linear);
		}

		private void FadeOut()
		{
			LeanTween.alphaCanvas(canvasGroup, 0f, transitionTime).setEase(LeanTweenType.linear);
		}

		public override void OnEnable()
		{
			StartCoroutine(PlayAnimation());
		}

		IEnumerator PlayAnimation()
		{
			FadeIn();
			yield return WaitFor.Seconds(transitionTime);
			yield return WaitFor.Seconds(stayTime);
			FadeOut();
			yield return WaitFor.Seconds(transitionTime);
			gameObject.SetActive(false);
		}
	}
}