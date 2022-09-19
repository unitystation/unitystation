using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Learning
{
	public class ProtipUI : MonoBehaviour
	{
		[SerializeField] private TMP_Text tipText;
		[SerializeField] private Image tipImage;
		[SerializeField] private Sprite defaultTipImage;
		[SerializeField] private bool isOnRightSide = false;

		private bool isShown = false;
		private ProtipSO currentTip;

		public enum SpriteAnimation
		{
			NONE,
			ROCKING,
			UPDOWN
		}

		private void OnEnable()
		{
			transform.localScale = Vector3.zero;
		}

		public void ShowTip(ProtipSO tip)
		{
			StopAllCoroutines();
			SetPositionInTransform();
			tipText.text = tip.TipData.Tip;
			if (tip.TipData.TipIcon != null) tipImage.sprite = tip.TipData.TipIcon;
			StartCoroutine(TipShow(tip.TipData.ShowDuration));
			switch (tip.TipData.ShowAnimation)
			{
				case SpriteAnimation.ROCKING:
					StartCoroutine(DoImageRockAnimations());
					break;
				case SpriteAnimation.UPDOWN:
					StartCoroutine(DoImageJumpAnimations());
					break;
				default:
					break;
			}
			currentTip = tip;
		}

		private void SetPositionInTransform()
		{
			tipImage.transform.SetSiblingIndex(isOnRightSide ? 0 : 1);
		}

		private IEnumerator TipShow(float duration)
		{
			LeanTween.scale(gameObject, Vector3.one, 1f).setEase(LeanTweenType.easeOutBounce);
			isShown = true;
			yield return WaitFor.Seconds(duration);
			StartCoroutine(TipHide());
		}

		private IEnumerator TipHide()
		{
			isShown = false;
			LeanTween.scale(gameObject, Vector3.zero, 0.7f).setEase(LeanTweenType.easeInBounce);
			yield return WaitFor.Seconds(1.5f);
			ProtipManager.Instance.IsShowingTip = false;
			gameObject.SetActive(false);
		}

		private IEnumerator DoImageRockAnimations()
		{
			var rot = 10;
			var halfRockDone = false;
			var rockTime = 0.65f;

			while (isShown)
			{
				LeanTween.rotateZ(tipImage.gameObject, halfRockDone ? -rot : rot, rockTime);
				yield return WaitFor.Seconds(rockTime);
				halfRockDone = !halfRockDone;
			}
		}

		private IEnumerator DoImageJumpAnimations()
		{
			var rot = 5;
			var halfRockDone = false;
			var rockTime = 0.5f;

			while (isShown)
			{
				LeanTween.moveLocalY(tipImage.gameObject, halfRockDone ? -rot : rot, rockTime);
				yield return WaitFor.Seconds(rockTime);
				halfRockDone = !halfRockDone;
			}
		}

		public void RememberTip()
		{
			PlayerPrefs.SetString($"{currentTip.TipTitle}", "true");
			PlayerPrefs.Save();
			StopCoroutine(nameof(ShowTip));
			StartCoroutine(TipHide());
		}
	}
}