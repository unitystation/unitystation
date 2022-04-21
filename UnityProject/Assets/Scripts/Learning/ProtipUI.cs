using System;
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

		public enum SpriteAnimation
		{
			NONE,
			ROCKING,
			UPDOWN
		}

		public void ShowTip(string tip, float showDuration = 25f, Sprite img = null, SpriteAnimation animation = SpriteAnimation.ROCKING)
		{
			SetPositionInTransform();
			gameObject.SetActive(true);
			tipText.text = tip;
			if (img != null) tipImage.sprite = img;
			StartCoroutine(TipShowCooldown(showDuration));
			switch (animation)
			{
				case SpriteAnimation.NONE:
					break;
				case SpriteAnimation.ROCKING:
					StartCoroutine(DoImageRockAnimations());
					break;
				case SpriteAnimation.UPDOWN:
					StartCoroutine(DoImageJumpAnimations());
					break;
			}
		}

		private void SetPositionInTransform()
		{
			tipImage.transform.SetSiblingIndex(isOnRightSide ? 0 : 1);
		}

		private IEnumerator TipShowCooldown(float duration)
		{
			LeanTween.scale(gameObject, Vector3.one, 0.5f);
			isShown = true;
			yield return WaitFor.Seconds(duration);
			isShown = false;
			LeanTween.scale(gameObject, Vector3.zero, 0.5f);
			gameObject.SetActive(false);
		}

		private IEnumerator DoImageRockAnimations()
		{
			var rot = 40;
			var halfRockDone = false;
			var rockTime = 0.5f;

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
	}
}