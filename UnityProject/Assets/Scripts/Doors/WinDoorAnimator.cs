using System.Collections;
using UnityEngine;

namespace Doors
{
	/// <summary>
	/// Window door animator. For controlling glass sliding door
	/// animations.
	/// </summary>
	public class WinDoorAnimator : DoorAnimator
	{
		public DoorDirection direction;

		public enum DoorDirection
		{
			SOUTH,
			NORTH,
			EAST,
			WEST
		};

		private int openFrame = 0;
		private int closeFrame = 76;
		private int deniedFrame = 80;
		private int[] animFrames = { 48, 36, 32, 28, 20, 16 };
		private SpriteRenderer doorbase;
		private Sprite[] sprites;

		public void Awake()
		{
			sprites = Resources.LoadAll<Sprite>("icons/obj/doors/windoor");
			foreach (Transform child in transform) {
				switch (child.gameObject.name) {
					case "doorbase":
						doorbase = (child.gameObject.GetComponent<SpriteRenderer>());
						break;
				}
			}
			doorbase.sprite = sprites[closeFrame + (int)direction];
		}

		public override void OpenDoor()
		{
			doorController.isPerformingAction = true;
			doorController.PlayOpenSound();
			doorController.isPerformingAction = false;
			StartCoroutine(PlayOpenAnim());
		}

		public override void CloseDoor()
		{
			doorController.isPerformingAction = true;
			doorController.PlayCloseSound();
			StartCoroutine(PlayCloseAnim());
		}

		public override void AccessDenied()
		{
			doorController.isPerformingAction = true;
			SoundManager.PlayAtPosition("AccessDenied", transform.position);
			StartCoroutine(PlayDeniedAnim());
		}

		IEnumerator Delay()
		{
			yield return new WaitForSeconds(0.3f);
			doorController.isPerformingAction = false;
		}

		IEnumerator PlayCloseAnim()
		{
			for (int i = animFrames.Length - 1; i >= 0; i--) {
				doorbase.sprite = sprites[animFrames[i] + (int)direction];
				//Stop movement half way through door opening to sync up with sortingOrder layer change
				if (i == 3) {
					doorController.BoxCollToggleOn();
				}
				yield return new WaitForSeconds(0.1f);
			}
			doorbase.sprite = sprites[closeFrame + (int)direction];
			doorController.isPerformingAction = false;
		}

		IEnumerator PlayOpenAnim()
		{
			for (int j = 0; j < animFrames.Length; j++) {
				doorbase.sprite = sprites[animFrames[j] + (int)direction];
				//Allow movement half way through door opening to sync up with sortingOrder layer change
				if (j == 3) {
					doorController.BoxCollToggleOff();
				}
				yield return new WaitForSeconds(0.1f);
			}

			doorbase.sprite = sprites[openFrame + (int)direction];
			doorController.isPerformingAction = false;
		}


		IEnumerator PlayDeniedAnim()
		{
			bool light = false;
			for (int i = 0; i < animFrames.Length * 2; i++) {
				if (!light) {
					doorbase.sprite = sprites[deniedFrame + (int)direction];
				} else {
					doorbase.sprite = sprites[closeFrame + (int)direction];
				}
				yield return new WaitForSeconds(0.05f);
			}
			doorbase.sprite = sprites[closeFrame + (int)direction];
			doorController.isPerformingAction = false;
		}
	}
}
