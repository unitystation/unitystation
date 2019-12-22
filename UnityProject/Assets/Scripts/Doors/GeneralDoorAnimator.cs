using System.Collections;
using UnityEngine;

/// <summary>
/// Used for shutters, win doors or anything that just needs a
/// general door animator.
/// Remember to name at least one of the child sprite renderers as 'DoorBase'
/// </summary>
public class GeneralDoorAnimator : DoorAnimator
{
	[Tooltip("The resource path to the sprite sheet. i.e: icons/obj/doors/windoor")]
	public string spritePath;

	public int[] animFrames;
	public int closeFrame;
	public int deniedFrame;
	public int openFrame;

	public DoorDirection direction;
	public bool IncludeAccessDeniedAnim;
	private SpriteRenderer doorbase;
	private Sprite[] sprites;

	public void Awake()
	{
		sprites = Resources.LoadAll<Sprite>(spritePath);
		foreach (Transform child in transform)
		{
			var cn = child.name.ToUpper();
			if(cn.Contains("DOORBASE")) doorbase = child.gameObject.GetComponent<SpriteRenderer>();
		}

		doorbase.sprite = sprites[closeFrame + (int) direction];
	}

	public override void OpenDoor(bool skipAnimation)
	{
		if (!skipAnimation)
		{
			doorController.isPerformingAction = true;
			doorController.PlayOpenSound();
			doorController.isPerformingAction = false;
		}

		StartCoroutine(PlayOpenAnim(skipAnimation));
	}

	public override void CloseDoor(bool skipAnimation)
	{
		if (!skipAnimation)
		{
			doorController.isPerformingAction = true;
			doorController.PlayCloseSound();
		}

		StartCoroutine(PlayCloseAnim(skipAnimation));
	}

	public override void AccessDenied(bool skipAnimation)
	{
		if (skipAnimation || !IncludeAccessDeniedAnim)
		{
			return;
		}

		doorController.isPerformingAction = true;
		SoundManager.PlayAtPosition("AccessDenied", transform.position);
		StartCoroutine(PlayDeniedAnim());
	}

	private IEnumerator Delay()
	{
		yield return WaitFor.Seconds(0.3f);
		doorController.isPerformingAction = false;
	}

	private IEnumerator PlayCloseAnim(bool skipAnimation)
	{
		if (skipAnimation)
		{
			doorController.BoxCollToggleOn();
		}
		else
		{
			for (int i = animFrames.Length - 1; i >= 0; i--)
			{
				doorbase.sprite = sprites[animFrames[i] + (int) direction];
				//Stop movement half way through door opening to sync up with sortingOrder layer change
				if (i == 3)
				{
					doorController.BoxCollToggleOn();
				}

				yield return WaitFor.Seconds(0.1f);
			}
		}

		doorbase.sprite = sprites[closeFrame + (int) direction];
		doorController.OnAnimationFinished();
	}

	private IEnumerator PlayOpenAnim(bool skipAnimation)
	{
		if (skipAnimation)
		{
			doorbase.sprite = sprites[animFrames[animFrames.Length - 1] + (int) direction];
			doorController.BoxCollToggleOff();
		}
		else
		{
			for (int j = 0; j < animFrames.Length; j++)
			{
				doorbase.sprite = sprites[animFrames[j] + (int) direction];
				//Allow movement half way through door opening to sync up with sortingOrder layer change
				if (j == 3)
				{
					doorController.BoxCollToggleOff();
				}

				yield return WaitFor.Seconds(0.1f);
			}
		}

		doorbase.sprite = sprites[openFrame + (int) direction];
		doorController.OnAnimationFinished();
	}


	private IEnumerator PlayDeniedAnim()
	{
		bool light = false;
		for (int i = 0; i < animFrames.Length * 2; i++)
		{
			if (!light)
			{
				doorbase.sprite = sprites[deniedFrame + (int) direction];
			}
			else
			{
				doorbase.sprite = sprites[closeFrame + (int) direction];
			}

			light = !light;
			yield return WaitFor.Seconds(0.05f);
		}

		doorbase.sprite = sprites[closeFrame + (int) direction];
		doorController.OnAnimationFinished();
	}
}

public enum DoorDirection
{
	SOUTH,
	NORTH,
	EAST,
	WEST
}